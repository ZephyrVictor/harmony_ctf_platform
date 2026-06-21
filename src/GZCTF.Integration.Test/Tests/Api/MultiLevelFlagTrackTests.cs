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
/// Integration tests for TrackController CRUD endpoints.
/// Covers Bug 1 (MaxLength validation) and basic track lifecycle.
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public class MultiLevelFlagTrackTests(GZCTFApplicationFactory factory)
{
    private async Task<(HttpClient client, int gameId, int challengeId)> SetupAsync()
    {
        var password = "Admin@Pass123";
        var admin = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), password, role: Role.Admin);

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Track CRUD Test Game");
        var challenge = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id,
            "Track CRUD Challenge", "flag{seed}");

        var client = factory.CreateClient();
        var loginResp = await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = admin.UserName, Password = password });
        loginResp.EnsureSuccessStatusCode();

        return (client, game.Id, challenge.Id);
    }

    [Fact]
    public async Task CreateTrack_Returns201_WithGeneratedId()
    {
        var (client, _, challengeId) = await SetupAsync();

        var response = await client.PostAsJsonAsync(
            $"/api/ext/challenges/{challengeId}/tracks",
            new TrackCreateModel { Title = "Test Track", FlawNo = "CVE-2026-0001", IsEnabled = true });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var track = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotEqual(Guid.Empty, track.GetProperty("id").GetGuid());
        Assert.Equal("Test Track", track.GetProperty("title").GetString());
        Assert.Equal("CVE-2026-0001", track.GetProperty("flawNo").GetString());
        Assert.True(track.GetProperty("isEnabled").GetBoolean());
    }

    [Fact]
    public async Task CreateTrack_WithLongTitle_Returns400()
    {
        // Bug 1: title > 200 chars should be rejected by [MaxLength(200)] data annotation
        var (client, _, challengeId) = await SetupAsync();

        var longTitle = new string('x', 250);
        var response = await client.PostAsJsonAsync(
            $"/api/ext/challenges/{challengeId}/tracks",
            new TrackCreateModel { Title = longTitle });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetTrack_NonExistentId_Returns404()
    {
        var (client, _, challengeId) = await SetupAsync();

        var response = await client.GetAsync(
            $"/api/ext/challenges/{challengeId}/tracks/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTrack_ModifiesFields()
    {
        var (client, _, challengeId) = await SetupAsync();

        var createResp = await client.PostAsJsonAsync(
            $"/api/ext/challenges/{challengeId}/tracks",
            new TrackCreateModel { Title = "Original Title" });
        createResp.EnsureSuccessStatusCode();
        var track = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var trackId = track.GetProperty("id").GetGuid();

        var updateResp = await client.PutAsJsonAsync(
            $"/api/ext/challenges/{challengeId}/tracks/{trackId}",
            new TrackUpdateModel { Title = "Updated Title", FlawNo = "CVE-2026-9999" });
        updateResp.EnsureSuccessStatusCode();
        var updated = await updateResp.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("Updated Title", updated.GetProperty("title").GetString());
        Assert.Equal("CVE-2026-9999", updated.GetProperty("flawNo").GetString());
    }

    [Fact]
    public async Task DeleteTrack_SoftDeletesBySettingIsEnabledFalse()
    {
        var (client, _, challengeId) = await SetupAsync();

        var createResp = await client.PostAsJsonAsync(
            $"/api/ext/challenges/{challengeId}/tracks",
            new TrackCreateModel { Title = "To Delete" });
        createResp.EnsureSuccessStatusCode();
        var track = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var trackId = track.GetProperty("id").GetGuid();

        var deleteResp = await client.DeleteAsync(
            $"/api/ext/challenges/{challengeId}/tracks/{trackId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResp.StatusCode);

        // Verify IsEnabled=false in DB (soft delete)
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dbTrack = await context.ChallengeTracks.FirstOrDefaultAsync(t => t.Id == trackId);
        Assert.NotNull(dbTrack);
        Assert.False(dbTrack!.IsEnabled);
    }

    [Fact]
    public async Task DeleteTrack_Idempotent_Returns204OnSecondCall()
    {
        // Soft delete is idempotent: track remains in DB with IsEnabled=false,
        // so subsequent DeleteTrack calls still find and "delete" it.
        var (client, _, challengeId) = await SetupAsync();

        var createResp = await client.PostAsJsonAsync(
            $"/api/ext/challenges/{challengeId}/tracks",
            new TrackCreateModel { Title = "Delete Twice" });
        createResp.EnsureSuccessStatusCode();
        var track = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var trackId = track.GetProperty("id").GetGuid();

        var firstDelete = await client.DeleteAsync(
            $"/api/ext/challenges/{challengeId}/tracks/{trackId}");
        Assert.Equal(HttpStatusCode.NoContent, firstDelete.StatusCode);

        var secondDelete = await client.DeleteAsync(
            $"/api/ext/challenges/{challengeId}/tracks/{trackId}");
        Assert.Equal(HttpStatusCode.NoContent, secondDelete.StatusCode);
    }

    [Fact]
    public async Task CreateTrack_RequiresAdminAuth_Returns401WithoutLogin()
    {
        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "No Auth Game");
        var challenge = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id,
            "No Auth Challenge", "flag{x}");

        using var client = factory.CreateClient(); // no login

        var response = await client.PostAsJsonAsync(
            $"/api/ext/challenges/{challenge.Id}/tracks",
            new TrackCreateModel { Title = "Should Fail" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
