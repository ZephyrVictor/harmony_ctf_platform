using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GZCTF.Controllers;
using GZCTF.Integration.Test.Base;
using GZCTF.Models;
using GZCTF.Models.Data;
using GZCTF.Models.Request.Account;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GZCTF.Integration.Test.Tests.Api;

/// <summary>
/// Integration tests for LevelConfigController CRUD endpoints.
/// Covers Bug 2 (Range validation) and Bug 13 (HasFlags filters by IsEnabled).
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public class MultiLevelFlagLevelConfigTests(GZCTFApplicationFactory factory)
{
    private async Task<(HttpClient client, int gameId, int challengeId)> SetupAsync()
    {
        var password = "Admin@Pass123";
        var admin = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), password, role: Role.Admin);

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "LevelConfig CRUD Test Game");
        var challenge = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id,
            "LevelConfig CRUD Challenge", "flag{seed}");

        var client = factory.CreateClient();
        var loginResp = await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = admin.UserName, Password = password });
        loginResp.EnsureSuccessStatusCode();

        return (client, game.Id, challenge.Id);
    }

    [Fact]
    public async Task CreateLevel_Returns201()
    {
        var (client, _, challengeId) = await SetupAsync();

        var response = await client.PostAsJsonAsync(
            $"/api/ext/challenges/{challengeId}/levels",
            new LevelConfigCreateModel { Level = 2, Title = "Level 2", Score = 200 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var config = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, config.GetProperty("level").GetInt32());
        Assert.Equal(200, config.GetProperty("score").GetInt32());
    }

    [Fact]
    public async Task CreateLevel_WithNegativeScore_Returns400()
    {
        // Bug 2: score=-100 should be rejected by [Range(0, int.MaxValue)]
        var (client, _, challengeId) = await SetupAsync();

        var response = await client.PostAsJsonAsync(
            $"/api/ext/challenges/{challengeId}/levels",
            new LevelConfigCreateModel { Level = 2, Score = -100 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateLevel_DuplicateLevel_Returns409()
    {
        var (client, _, challengeId) = await SetupAsync();

        var firstResp = await client.PostAsJsonAsync(
            $"/api/ext/challenges/{challengeId}/levels",
            new LevelConfigCreateModel { Level = 3, Score = 300 });
        firstResp.EnsureSuccessStatusCode();

        var secondResp = await client.PostAsJsonAsync(
            $"/api/ext/challenges/{challengeId}/levels",
            new LevelConfigCreateModel { Level = 3, Score = 999 });

        Assert.Equal(HttpStatusCode.Conflict, secondResp.StatusCode);
    }

    [Fact]
    public async Task DeleteLevel_WithExistingFlags_Returns409()
    {
        // Bug 13 variant: active flags (IsEnabled=true) block level deletion
        var (client, _, challengeId) = await SetupAsync();

        // Create level config
        var levelResp = await client.PostAsJsonAsync(
            $"/api/ext/challenges/{challengeId}/levels",
            new LevelConfigCreateModel { Level = 2, Score = 200 });
        levelResp.EnsureSuccessStatusCode();
        var level = await levelResp.Content.ReadFromJsonAsync<JsonElement>();
        var levelId = level.GetProperty("id").GetInt32();

        // Create a track + flag at this level
        var trackResp = await client.PostAsJsonAsync(
            $"/api/ext/challenges/{challengeId}/tracks",
            new TrackCreateModel { Title = "Track for Level 2" });
        trackResp.EnsureSuccessStatusCode();
        var track = await trackResp.Content.ReadFromJsonAsync<JsonElement>();
        var trackId = track.GetProperty("id").GetGuid();

        var flagResp = await client.PostAsJsonAsync(
            $"/api/ext/challenges/{challengeId}/flags",
            new ChallengeFlagCreateModel
            {
                FlagValue = "flag{level2}",
                Level = 2,
                TrackId = trackId
            });
        flagResp.EnsureSuccessStatusCode();

        // Try to delete the level — should be blocked (409)
        var deleteResp = await client.DeleteAsync(
            $"/api/ext/challenges/{challengeId}/levels/{levelId}");
        Assert.Equal(HttpStatusCode.Conflict, deleteResp.StatusCode);
    }

    [Fact]
    public async Task DeleteLevel_WithSoftDeletedFlags_Returns204()
    {
        // Bug 13 fix: HasFlags filters by IsEnabled=true. When DeleteTrack cascades
        // soft-delete to its flags (IsEnabled=false), HasFlags returns false and
        // the level can be deleted.
        var (client, _, challengeId) = await SetupAsync();

        var levelResp = await client.PostAsJsonAsync(
            $"/api/ext/challenges/{challengeId}/levels",
            new LevelConfigCreateModel { Level = 2, Score = 200 });
        levelResp.EnsureSuccessStatusCode();
        var level = await levelResp.Content.ReadFromJsonAsync<JsonElement>();
        var levelId = level.GetProperty("id").GetInt32();

        var trackResp = await client.PostAsJsonAsync(
            $"/api/ext/challenges/{challengeId}/tracks",
            new TrackCreateModel { Title = "Track" });
        trackResp.EnsureSuccessStatusCode();
        var track = await trackResp.Content.ReadFromJsonAsync<JsonElement>();
        var trackId = track.GetProperty("id").GetGuid();

        var flagResp = await client.PostAsJsonAsync(
            $"/api/ext/challenges/{challengeId}/flags",
            new ChallengeFlagCreateModel
            {
                FlagValue = "flag{todelete}",
                Level = 2,
                TrackId = trackId
            });
        flagResp.EnsureSuccessStatusCode();

        // Soft-delete the track, which cascades IsEnabled=false to its flags
        var trackDelResp = await client.DeleteAsync(
            $"/api/ext/challenges/{challengeId}/tracks/{trackId}");
        Assert.Equal(HttpStatusCode.NoContent, trackDelResp.StatusCode);

        // Now delete the level — should succeed because flags are soft-deleted
        var deleteResp = await client.DeleteAsync(
            $"/api/ext/challenges/{challengeId}/levels/{levelId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResp.StatusCode);
    }

    [Fact]
    public async Task UpdateLevel_ModifiesFields()
    {
        var (client, _, challengeId) = await SetupAsync();

        var createResp = await client.PostAsJsonAsync(
            $"/api/ext/challenges/{challengeId}/levels",
            new LevelConfigCreateModel { Level = 2, Score = 200, Title = "Old" });
        createResp.EnsureSuccessStatusCode();
        var level = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var levelId = level.GetProperty("id").GetInt32();

        var updateResp = await client.PutAsJsonAsync(
            $"/api/ext/challenges/{challengeId}/levels/{levelId}",
            new LevelConfigUpdateModel { Title = "New Title", Score = 500 });
        updateResp.EnsureSuccessStatusCode();
        var updated = await updateResp.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("New Title", updated.GetProperty("title").GetString());
        Assert.Equal(500, updated.GetProperty("score").GetInt32());
    }

    [Fact]
    public async Task GetLevel_NonExistentId_Returns404()
    {
        var (client, _, challengeId) = await SetupAsync();

        var response = await client.GetAsync(
            $"/api/ext/challenges/{challengeId}/levels/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
