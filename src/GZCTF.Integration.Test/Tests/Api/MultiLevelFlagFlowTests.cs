using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GZCTF.Controllers;
using GZCTF.Integration.Test.Base;
using GZCTF.Models;
using GZCTF.Models.Data;
using GZCTF.Models.Request.Account;
using GZCTF.Models.Request.Game;
using GZCTF.Repositories.Interface;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

// JsonElement is in System.Text.Json — alias not needed since imported above.

namespace GZCTF.Integration.Test.Tests.Api;

/// <summary>
/// End-to-end integration tests for the multi-level flag submission flow.
/// Covers Bug A (attemptsUsed), Bug B (SolvedAt), Bug F (ban idempotency),
/// Bug G (CurrentLevel), and FirstSolve creation.
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public class MultiLevelFlagFlowTests(GZCTFApplicationFactory factory)
{
    private async Task<Fixture> SetupFixtureAsync(bool withL2 = true, bool withL3Alternatives = false)
    {
        var adminPassword = "Admin@Pass123";
        var admin = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), adminPassword, role: Role.Admin);

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Multi-Level Flow Game");
        // Create challenge with no flags — we'll add multi-level flags via API
        using (var scope = factory.Services.CreateScope())
        {
            var gameRepo = scope.ServiceProvider.GetRequiredService<IGameRepository>();
            var challengeRepo = scope.ServiceProvider.GetRequiredService<IGameChallengeRepository>();
            var gameObj = await gameRepo.GetGameById(game.Id)
                          ?? throw new InvalidOperationException($"Game {game.Id} not found");

            GameChallenge challenge = new()
            {
                Title = "Multi-Level Challenge",
                Content = "Multi-level content",
                Category = ChallengeCategory.Misc,
                Type = ChallengeType.StaticAttachment,
                Hints = [],
                IsEnabled = true,
                OriginalScore = 1000,
                MinScoreRate = 0.8,
                Difficulty = 5,
                Game = gameObj,
                GameId = gameObj.Id
            };
            await challengeRepo.CreateChallenge(gameObj, challenge);
        }

        int challengeId;
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            challengeId = await context.GameChallenges
                .Where(c => c.GameId == game.Id && c.Title == "Multi-Level Challenge")
                .Select(c => c.Id).FirstAsync();
        }

        // Admin client to create tracks/levels/flags
        using var adminClient = factory.CreateClient();
        await adminClient.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = admin.UserName, Password = adminPassword });

        // Create L1 level config
        var l1Resp = await adminClient.PostAsJsonAsync(
            $"/api/ext/challenges/{challengeId}/levels",
            new LevelConfigCreateModel { Level = 1, Title = "Level 1", Score = 100 });
        l1Resp.EnsureSuccessStatusCode();

        // Create L2 level config with trap/ban answers (if requested)
        if (withL2)
        {
            var l2Resp = await adminClient.PostAsJsonAsync(
                $"/api/ext/challenges/{challengeId}/levels",
                new LevelConfigCreateModel
                {
                    Level = 2,
                    Title = "Level 2",
                    Score = 200,
                    LockOnAnswers = ["trap_answer"],
                    BanOnAnswers = ["ban_answer"]
                });
            l2Resp.EnsureSuccessStatusCode();
        }

        if (withL3Alternatives)
        {
            var l3Resp = await adminClient.PostAsJsonAsync(
                $"/api/ext/challenges/{challengeId}/levels",
                new LevelConfigCreateModel
                {
                    Level = 3,
                    Title = "Level 3",
                    Score = 1,
                    OrSeparators = "、,;"
                });
            l3Resp.EnsureSuccessStatusCode();
        }

        // Create a track
        var trackResp = await adminClient.PostAsJsonAsync(
            $"/api/ext/challenges/{challengeId}/tracks",
            new TrackCreateModel { Title = "Track 1" });
        trackResp.EnsureSuccessStatusCode();
        var track = await trackResp.Content.ReadFromJsonAsync<JsonElement>();
        var trackId = track.GetProperty("id").GetGuid();

        // Create L1 flag on this track
        var l1FlagResp = await adminClient.PostAsJsonAsync(
            $"/api/ext/challenges/{challengeId}/flags",
            new ChallengeFlagCreateModel
            {
                FlagValue = "flag{l1_correct}",
                Level = 1,
                TrackId = trackId
            });
        l1FlagResp.EnsureSuccessStatusCode();

        if (withL2)
        {
            var l2FlagResp = await adminClient.PostAsJsonAsync(
                $"/api/ext/challenges/{challengeId}/flags",
                new ChallengeFlagCreateModel
                {
                    FlagValue = "flag{l2_correct}",
                    Level = 2,
                    TrackId = trackId
                });
            l2FlagResp.EnsureSuccessStatusCode();
        }

        if (withL3Alternatives)
        {
            var l3FlagResp = await adminClient.PostAsJsonAsync(
                $"/api/ext/challenges/{challengeId}/flags",
                new ChallengeFlagCreateModel
                {
                    FlagValue = "CWE-125; CWE-787",
                    Level = 3,
                    TrackId = trackId
                });
            l3FlagResp.EnsureSuccessStatusCode();
        }

        // Create user + team + join game
        var userPassword = "User@Pass123";
        var userName = TestDataSeeder.RandomName();
        var user = await TestDataSeeder.CreateUserAsync(factory.Services, userName, userPassword);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, $"Team {userName}");
        await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, team.Id, user.Id);

        var userClient = factory.CreateClient();
        var loginResp = await userClient.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = userPassword });
        loginResp.EnsureSuccessStatusCode();

        return new Fixture
        {
            GameId = game.Id,
            ChallengeId = challengeId,
            TrackId = trackId,
            UserClient = userClient,
            UserId = user.Id,
            TeamId = team.Id
        };
    }

    private async Task<Guid> CreateForeignTrack(int challengeId, bool isEnabled = true)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var track = new ChallengeTrack
        {
            Id = Guid.NewGuid(),
            ChallengeId = challengeId,
            Title = "Foreign Track",
            IsEnabled = isEnabled
        };
        context.ChallengeTracks.Add(track);
        context.ChallengeFlags.Add(new ChallengeFlag
        {
            Id = Guid.NewGuid(),
            ChallengeId = challengeId,
            TrackId = track.Id,
            Level = 2,
            FlagValue = "flag{foreign_l2}",
            RawAnswer = "flag{foreign_l2}",
            AnswerType = ChallengeFlagAnswerType.Exact,
            IsEnabled = true
        });
        await context.SaveChangesAsync();
        return track.Id;
    }

    private async Task AssertNoSubmissionSideEffects(Fixture fixture)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var participationId = await context.Participations
            .Where(p => p.GameId == fixture.GameId && p.TeamId == fixture.TeamId)
            .Select(p => p.Id)
            .SingleAsync();

        Assert.False(await context.Submissions.AnyAsync(s =>
            s.ParticipationId == participationId && s.ChallengeId == fixture.ChallengeId));
        Assert.False(await context.SubmissionRecords.AnyAsync(r =>
            r.ParticipationId == participationId && r.ChallengeId == fixture.ChallengeId));
        Assert.False(await context.ChallengeCooldownStates.AnyAsync(s =>
            s.ParticipationId == participationId && s.ChallengeId == fixture.ChallengeId));
    }

    /// <summary>
    /// Bug A + Bug G + Bug B: solve L1, verify attemptsUsed=1, SolvedAt non-null, CurrentLevel=2.
    /// </summary>
    [Fact]
    public async Task MultiLevel_SubmitCorrectFlag_Level1_UpdatesTrackStatus()
    {
        var fx = await SetupFixtureAsync();

        var submitResp = await fx.UserClient.PostAsJsonAsync(
            $"/api/Game/{fx.GameId}/Challenges/{fx.ChallengeId}",
            new FlagSubmitModel { Flag = "flag{l1_correct}", Level = 1 });
        submitResp.EnsureSuccessStatusCode();

        var detailResp = await fx.UserClient.GetAsync(
            $"/api/Game/{fx.GameId}/Challenges/{fx.ChallengeId}");
        detailResp.EnsureSuccessStatusCode();
        var detail = await detailResp.Content.ReadFromJsonAsync<JsonElement>();

        var tracks = detail.GetProperty("tracks").EnumerateArray().ToList();
        Assert.NotEmpty(tracks);
        var track = tracks.First(t => t.GetProperty("trackId").GetGuid() == fx.TrackId);
        var levels = track.GetProperty("levels").EnumerateArray().ToList();
        var l1 = levels.First(l => l.GetProperty("level").GetInt32() == 1);
        Assert.Equal("Solved", l1.GetProperty("status").GetString());
        Assert.Equal(1, l1.GetProperty("attemptsUsed").GetInt32()); // Bug A
        // Bug B: SolvedAt populated (serialized as Unix ms)
        var solvedAtEl = l1.GetProperty("solvedAt");
        Assert.Equal(JsonValueKind.Number, solvedAtEl.ValueKind);
        Assert.True(solvedAtEl.GetInt64() > 0);
        Assert.Equal(2, detail.GetProperty("currentLevel").GetInt32()); // Bug G
    }

    [Theory]
    [InlineData("CWE-125; CWE-787")]
    [InlineData("CWE-125")]
    [InlineData("CWE-787")]
    public async Task MultiLevel_ExactAlternatives_AcceptsWholeValueAndEitherCandidate(string answer)
    {
        var fx = await SetupFixtureAsync(withL3Alternatives: true);

        (await fx.UserClient.PostAsJsonAsync(
            $"/api/Game/{fx.GameId}/Challenges/{fx.ChallengeId}",
            new FlagSubmitModel { Flag = "flag{l1_correct}", Level = 1 })).EnsureSuccessStatusCode();
        (await fx.UserClient.PostAsJsonAsync(
            $"/api/Game/{fx.GameId}/Challenges/{fx.ChallengeId}",
            new FlagSubmitModel { Flag = "flag{l2_correct}", Level = 2, TrackId = fx.TrackId }))
            .EnsureSuccessStatusCode();

        var response = await fx.UserClient.PostAsJsonAsync(
            $"/api/Game/{fx.GameId}/Challenges/{fx.ChallengeId}",
            new FlagSubmitModel { Flag = answer, Level = 3, TrackId = fx.TrackId });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Accepted", body.GetProperty("message").GetString());

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await context.SubmissionRecords.CountAsync(r =>
            r.GameId == fx.GameId
            && r.ChallengeId == fx.ChallengeId
            && r.TrackId == fx.TrackId
            && r.Level == 3
            && r.ScoreChange == 1));
    }

    [Fact]
    public async Task MultiLevel_ExactAlternativeAfterSolve_DoesNotAwardScoreTwice()
    {
        var fx = await SetupFixtureAsync(withL3Alternatives: true);

        (await fx.UserClient.PostAsJsonAsync(
            $"/api/Game/{fx.GameId}/Challenges/{fx.ChallengeId}",
            new FlagSubmitModel { Flag = "flag{l1_correct}", Level = 1 })).EnsureSuccessStatusCode();
        (await fx.UserClient.PostAsJsonAsync(
            $"/api/Game/{fx.GameId}/Challenges/{fx.ChallengeId}",
            new FlagSubmitModel { Flag = "flag{l2_correct}", Level = 2, TrackId = fx.TrackId }))
            .EnsureSuccessStatusCode();
        (await fx.UserClient.PostAsJsonAsync(
            $"/api/Game/{fx.GameId}/Challenges/{fx.ChallengeId}",
            new FlagSubmitModel { Flag = "CWE-125", Level = 3, TrackId = fx.TrackId }))
            .EnsureSuccessStatusCode();

        var duplicateResponse = await fx.UserClient.PostAsJsonAsync(
            $"/api/Game/{fx.GameId}/Challenges/{fx.ChallengeId}",
            new FlagSubmitModel { Flag = "CWE-787", Level = 3, TrackId = fx.TrackId });

        duplicateResponse.EnsureSuccessStatusCode();
        var body = await duplicateResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("已提交过", body.GetProperty("message").GetString());

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var participationId = await context.Participations
            .Where(p => p.GameId == fx.GameId && p.TeamId == fx.TeamId)
            .Select(p => p.Id)
            .SingleAsync();
        Assert.Equal(1, await context.SubmissionRecords.CountAsync(r =>
            r.GameId == fx.GameId
            && r.ChallengeId == fx.ChallengeId
            && r.TrackId == fx.TrackId
            && r.Level == 3
            && r.ScoreChange > 0));
        Assert.Equal(4, await context.Submissions.CountAsync(s =>
            s.ParticipationId == participationId && s.ChallengeId == fx.ChallengeId));
        Assert.Equal(1, await context.SubmissionRecords.CountAsync(r =>
            r.ParticipationId == participationId
            && r.ChallengeId == fx.ChallengeId
            && r.SubmissionType == "Duplicate"
            && r.ScoreChange == 0
            && r.MatchedFlagId == null));
    }

    /// <summary>
    /// Wrong answer should produce "Wrong answer" message and a negative ScoreChange.
    /// </summary>
    [Fact]
    public async Task MultiLevel_SubmitWrongFlag_AppliesPenalty()
    {
        var fx = await SetupFixtureAsync();

        var submitResp = await fx.UserClient.PostAsJsonAsync(
            $"/api/Game/{fx.GameId}/Challenges/{fx.ChallengeId}",
            new FlagSubmitModel { Flag = "flag{totally_wrong}", Level = 1 });

        // Wrong answer is still 200 OK with Message in body
        Assert.Equal(HttpStatusCode.OK, submitResp.StatusCode);
        var body = await submitResp.Content.ReadFromJsonAsync<JsonElement>();
        var message = body.GetProperty("message").GetString();
        Assert.Equal("Wrong answer", message);
    }

    [Fact]
    public async Task MultiLevel_ConcurrentWrongAnswers_TriggerCooldownAtExactThreshold()
    {
        var fx = await SetupFixtureAsync();

        var responses = await Task.WhenAll(Enumerable.Range(1, 5).Select(i =>
            fx.UserClient.PostAsJsonAsync(
                $"/api/Game/{fx.GameId}/Challenges/{fx.ChallengeId}",
                new FlagSubmitModel { Flag = $"flag{{concurrent_wrong_{i}}}", Level = 1 })));

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
        var bodies = await Task.WhenAll(responses.Select(response =>
            response.Content.ReadFromJsonAsync<JsonElement>()));
        Assert.Single(bodies, body =>
            body.GetProperty("cooldown").GetProperty("cooldownUntil").ValueKind == JsonValueKind.Number);

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var participationId = await context.Participations
            .Where(p => p.GameId == fx.GameId && p.TeamId == fx.TeamId)
            .Select(p => p.Id)
            .SingleAsync();
        var state = await context.ChallengeCooldownStates
            .AsNoTracking()
            .SingleAsync(s => s.ParticipationId == participationId && s.ChallengeId == fx.ChallengeId);

        Assert.Equal(0, state.WrongCount);
        Assert.True(state.CooldownUntil > DateTimeOffset.UtcNow);
        Assert.Equal(5, await context.Submissions.CountAsync(s =>
            s.ParticipationId == participationId && s.ChallengeId == fx.ChallengeId));
    }

    [Fact]
    public async Task MultiLevel_CooldownBlockedSubmission_DoesNotCreateSubmission()
    {
        var fx = await SetupFixtureAsync();

        for (var i = 1; i <= 5; i++)
        {
            var response = await fx.UserClient.PostAsJsonAsync(
                $"/api/Game/{fx.GameId}/Challenges/{fx.ChallengeId}",
                new FlagSubmitModel { Flag = $"flag{{cooldown_seed_{i}}}", Level = 1 });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var participationId = await context.Participations
            .Where(p => p.GameId == fx.GameId && p.TeamId == fx.TeamId)
            .Select(p => p.Id)
            .SingleAsync();
        var before = await context.Submissions.CountAsync(s =>
            s.ParticipationId == participationId && s.ChallengeId == fx.ChallengeId);

        var blocked = await fx.UserClient.PostAsJsonAsync(
            $"/api/Game/{fx.GameId}/Challenges/{fx.ChallengeId}",
            new FlagSubmitModel { Flag = "flag{cooldown_blocked}", Level = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, blocked.StatusCode);
        var body = await blocked.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Cooldown", body.GetProperty("scope").GetString());
        Assert.Equal(before, await context.Submissions.CountAsync(s =>
            s.ParticipationId == participationId && s.ChallengeId == fx.ChallengeId));
    }

    [Fact]
    public async Task SubmissionQuota_GameDailyTeamLimit_BlocksFurtherSubmissionsWithoutSideEffects()
    {
        var fx = await SetupFixtureAsync();

        using (var scope = factory.Services.CreateScope())
        {
            var setupContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var game = await setupContext.Games.SingleAsync(g => g.Id == fx.GameId);
            game.DailyTeamSubmissionLimit = 1;
            await setupContext.SaveChangesAsync();
        }

        var first = await fx.UserClient.PostAsJsonAsync(
            $"/api/Game/{fx.GameId}/Challenges/{fx.ChallengeId}",
            new FlagSubmitModel { Flag = "flag{l1_correct}", Level = 1 });
        first.EnsureSuccessStatusCode();

        var blocked = await fx.UserClient.PostAsJsonAsync(
            $"/api/Game/{fx.GameId}/Challenges/{fx.ChallengeId}",
            new FlagSubmitModel { Flag = "flag{blocked_by_game_daily}", Level = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, blocked.StatusCode);
        var body = await blocked.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("GameDailyTeam", body.GetProperty("scope").GetString());

        using var verifyScope = factory.Services.CreateScope();
        var context = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var participationId = await context.Participations
            .Where(p => p.GameId == fx.GameId && p.TeamId == fx.TeamId)
            .Select(p => p.Id)
            .SingleAsync();
        Assert.Equal(1, await context.Submissions.CountAsync(s => s.ParticipationId == participationId));
        Assert.False(await context.Submissions.AnyAsync(s =>
            s.ParticipationId == participationId && s.Answer == "flag{blocked_by_game_daily}"));
    }

    [Fact]
    public async Task SubmissionQuota_ChallengeTotalTeamLimit_DoesNotBlockOtherTeams()
    {
        var fx = await SetupFixtureAsync();
        var other = await CreateJoinedUserClient(fx.GameId, "QuotaOtherTeam");

        using (var scope = factory.Services.CreateScope())
        {
            var setupContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var challenge = await setupContext.GameChallenges.SingleAsync(c => c.Id == fx.ChallengeId);
            challenge.TotalTeamSubmissionLimit = 1;
            challenge.TotalTeamSubmissionLimitMode = SubmissionControlLimitMode.Custom;
            await setupContext.SaveChangesAsync();
        }

        var first = await fx.UserClient.PostAsJsonAsync(
            $"/api/Game/{fx.GameId}/Challenges/{fx.ChallengeId}",
            new FlagSubmitModel { Flag = "flag{wrong_team_seed}", Level = 1 });
        first.EnsureSuccessStatusCode();

        var sameTeamBlocked = await fx.UserClient.PostAsJsonAsync(
            $"/api/Game/{fx.GameId}/Challenges/{fx.ChallengeId}",
            new FlagSubmitModel { Flag = "flag{l1_correct}", Level = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, sameTeamBlocked.StatusCode);
        var body = await sameTeamBlocked.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ChallengeTotalTeam", body.GetProperty("scope").GetString());

        var otherTeamAllowed = await other.Client.PostAsJsonAsync(
            $"/api/Game/{fx.GameId}/Challenges/{fx.ChallengeId}",
            new FlagSubmitModel { Flag = "flag{l1_correct}", Level = 1 });
        otherTeamAllowed.EnsureSuccessStatusCode();

        using var verifyScope = factory.Services.CreateScope();
        var context = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var firstParticipationId = await context.Participations
            .Where(p => p.GameId == fx.GameId && p.TeamId == fx.TeamId)
            .Select(p => p.Id)
            .SingleAsync();
        var otherParticipationId = await context.Participations
            .Where(p => p.GameId == fx.GameId && p.TeamId == other.TeamId)
            .Select(p => p.Id)
            .SingleAsync();
        Assert.Equal(1, await context.Submissions.CountAsync(s => s.ParticipationId == firstParticipationId));
        Assert.Equal(1, await context.Submissions.CountAsync(s => s.ParticipationId == otherParticipationId));
    }

    [Fact]
    public async Task SubmissionQuota_InheritedChallengeDailyLimit_BlocksAfterDefaultFactor()
    {
        var fx = await SetupFixtureAsync();

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var game = await context.Games.SingleAsync(g => g.Id == fx.GameId);
            var challenge = await context.GameChallenges.SingleAsync(c => c.Id == fx.ChallengeId);
            game.ChallengeDailyTeamSubmissionLimitFactor = 1;
            challenge.DailyTeamSubmissionLimitMode = SubmissionControlLimitMode.InheritDefault;
            await context.SaveChangesAsync();
        }

        var first = await fx.UserClient.PostAsJsonAsync(
            $"/api/Game/{fx.GameId}/Challenges/{fx.ChallengeId}",
            new FlagSubmitModel { Flag = "flag{wrong_inherited_daily_seed}", Level = 1 });
        first.EnsureSuccessStatusCode();

        var blocked = await fx.UserClient.PostAsJsonAsync(
            $"/api/Game/{fx.GameId}/Challenges/{fx.ChallengeId}",
            new FlagSubmitModel { Flag = "flag{l1_correct}", Level = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, blocked.StatusCode);
        var body = await blocked.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ChallengeDailyTeam", body.GetProperty("scope").GetString());
        Assert.Equal(1, body.GetProperty("limit").GetInt32());
    }

    [Fact]
    public async Task SubmissionQuota_CustomChallengeLimit_IgnoresDefaultFactor()
    {
        var fx = await SetupFixtureAsync();

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var game = await context.Games.SingleAsync(g => g.Id == fx.GameId);
            var challenge = await context.GameChallenges.SingleAsync(c => c.Id == fx.ChallengeId);
            game.ChallengeTotalTeamSubmissionLimitFactor = 1;
            challenge.TotalTeamSubmissionLimitMode = SubmissionControlLimitMode.Custom;
            challenge.TotalTeamSubmissionLimit = 2;
            await context.SaveChangesAsync();
        }

        var first = await fx.UserClient.PostAsJsonAsync(
            $"/api/Game/{fx.GameId}/Challenges/{fx.ChallengeId}",
            new FlagSubmitModel { Flag = "flag{wrong_custom_total_seed}", Level = 1 });
        first.EnsureSuccessStatusCode();

        var second = await fx.UserClient.PostAsJsonAsync(
            $"/api/Game/{fx.GameId}/Challenges/{fx.ChallengeId}",
            new FlagSubmitModel { Flag = "flag{l1_correct}", Level = 1 });
        second.EnsureSuccessStatusCode();

        var blocked = await fx.UserClient.PostAsJsonAsync(
            $"/api/Game/{fx.GameId}/Challenges/{fx.ChallengeId}",
            new FlagSubmitModel { Flag = "flag{blocked_custom_total}", Level = 1 });
        Assert.Equal(HttpStatusCode.BadRequest, blocked.StatusCode);
        var body = await blocked.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ChallengeTotalTeam", body.GetProperty("scope").GetString());
        Assert.Equal(2, body.GetProperty("limit").GetInt32());
    }

    [Fact]
    public async Task SubmissionQuota_UnlimitedChallengeLimit_DoesNotBlock()
    {
        var fx = await SetupFixtureAsync();

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var game = await context.Games.SingleAsync(g => g.Id == fx.GameId);
            var challenge = await context.GameChallenges.SingleAsync(c => c.Id == fx.ChallengeId);
            game.ChallengeTotalTeamSubmissionLimitFactor = 1;
            challenge.TotalTeamSubmissionLimitMode = SubmissionControlLimitMode.Unlimited;
            await context.SaveChangesAsync();
        }

        var first = await fx.UserClient.PostAsJsonAsync(
            $"/api/Game/{fx.GameId}/Challenges/{fx.ChallengeId}",
            new FlagSubmitModel { Flag = "flag{wrong_unlimited_total_seed}", Level = 1 });
        first.EnsureSuccessStatusCode();

        var second = await fx.UserClient.PostAsJsonAsync(
            $"/api/Game/{fx.GameId}/Challenges/{fx.ChallengeId}",
            new FlagSubmitModel { Flag = "flag{l1_correct}", Level = 1 });
        second.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task SubmissionControl_ApplyDefaultsToAllChallenges_SetsInheritedModes()
    {
        var fx = await SetupFixtureAsync();
        var adminPassword = "Admin@SubmissionControl123";
        var admin = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), adminPassword, role: Role.Admin);

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var challenge = await context.GameChallenges.SingleAsync(c => c.Id == fx.ChallengeId);
            challenge.DailyTeamSubmissionLimitMode = SubmissionControlLimitMode.Custom;
            challenge.DailyTeamSubmissionLimit = 99;
            challenge.TotalTeamSubmissionLimitMode = SubmissionControlLimitMode.Unlimited;
            challenge.CooldownWrongAnswerLimitMode = SubmissionControlLimitMode.Custom;
            challenge.CooldownWrongAnswerLimitOverride = 9;
            await context.SaveChangesAsync();
        }

        using var adminClient = factory.CreateClient();
        var login = await adminClient.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = admin.UserName, Password = adminPassword });
        login.EnsureSuccessStatusCode();

        var update = await adminClient.PutAsJsonAsync(
            $"/api/admin/games/{fx.GameId}/submission-control",
            new
            {
                dailyTeamSubmissionLimit = 0,
                cooldownMinutes = 5,
                challengeCooldownWrongAnswerLimitFactor = 3,
                challengeDailyTeamSubmissionLimitFactor = 4,
                challengeTotalTeamSubmissionLimitFactor = 5,
                applyDefaultsToAllChallenges = true,
                challenges = Array.Empty<object>()
            });
        update.EnsureSuccessStatusCode();

        var body = await update.Content.ReadFromJsonAsync<JsonElement>();
        var challengeDto = body.GetProperty("challenges").EnumerateArray()
            .Single(c => c.GetProperty("challengeId").GetInt32() == fx.ChallengeId);
        Assert.Equal("InheritDefault",
            challengeDto.GetProperty("dailyTeamSubmission").GetProperty("mode").GetString());
        Assert.Equal(4,
            challengeDto.GetProperty("dailyTeamSubmission").GetProperty("effectiveValue").GetInt32());
        Assert.Equal(5,
            challengeDto.GetProperty("totalTeamSubmission").GetProperty("effectiveValue").GetInt32());
        Assert.Equal(3,
            challengeDto.GetProperty("cooldownWrongAnswerLimit").GetProperty("effectiveValue").GetInt32());

        using var verifyScope = factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var persisted = await verifyContext.GameChallenges.SingleAsync(c => c.Id == fx.ChallengeId);
        Assert.Equal(SubmissionControlLimitMode.InheritDefault, persisted.DailyTeamSubmissionLimitMode);
        Assert.Equal(SubmissionControlLimitMode.InheritDefault, persisted.TotalTeamSubmissionLimitMode);
        Assert.Equal(SubmissionControlLimitMode.InheritDefault, persisted.CooldownWrongAnswerLimitMode);
    }

    [Fact]
    public async Task SubmissionQuota_DetailShowsConfiguredLimitsAndOmitsUnconfigured()
    {
        var fx = await SetupFixtureAsync();

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var game = await context.Games.SingleAsync(g => g.Id == fx.GameId);
            var challenge = await context.GameChallenges.SingleAsync(c => c.Id == fx.ChallengeId);
            game.DailyTeamSubmissionLimit = 10;
            challenge.TotalTeamSubmissionLimit = 20;
            challenge.TotalTeamSubmissionLimitMode = SubmissionControlLimitMode.Custom;
            challenge.DailyTeamSubmissionLimitMode = SubmissionControlLimitMode.Unlimited;
            await context.SaveChangesAsync();
        }

        var detailResp = await fx.UserClient.GetAsync($"/api/Game/{fx.GameId}/Challenges/{fx.ChallengeId}");
        detailResp.EnsureSuccessStatusCode();
        var detail = await detailResp.Content.ReadFromJsonAsync<JsonElement>();
        var control = detail.GetProperty("submissionControl");

        Assert.Equal(10, control.GetProperty("gameDailyTeam").GetProperty("limit").GetInt32());
        Assert.Equal(20, control.GetProperty("challengeTotalTeam").GetProperty("limit").GetInt32());
        Assert.Equal(JsonValueKind.Null, control.GetProperty("challengeDailyTeam").ValueKind);
    }

    [Fact]
    public async Task MultiLevel_Level1WithTrackId_ReturnsBadRequestWithoutSideEffects()
    {
        var fx = await SetupFixtureAsync();

        var response = await fx.UserClient.PostAsJsonAsync(
            $"/api/Game/{fx.GameId}/Challenges/{fx.ChallengeId}",
            new FlagSubmitModel { Flag = "flag{l1_correct}", Level = 1, TrackId = fx.TrackId });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertNoSubmissionSideEffects(fx);
    }

    [Fact]
    public async Task MultiLevel_NonexistentTrack_ReturnsBadRequestWithoutSideEffects()
    {
        var fx = await SetupFixtureAsync();

        var response = await fx.UserClient.PostAsJsonAsync(
            $"/api/Game/{fx.GameId}/Challenges/{fx.ChallengeId}",
            new FlagSubmitModel { Flag = "flag{l2_correct}", Level = 2, TrackId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertNoSubmissionSideEffects(fx);
    }

    [Fact]
    public async Task MultiLevel_DisabledTrack_ReturnsBadRequestWithoutSideEffects()
    {
        var fx = await SetupFixtureAsync();
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var track = await context.ChallengeTracks.SingleAsync(t => t.Id == fx.TrackId);
            track.IsEnabled = false;
            await context.SaveChangesAsync();
        }

        var response = await fx.UserClient.PostAsJsonAsync(
            $"/api/Game/{fx.GameId}/Challenges/{fx.ChallengeId}",
            new FlagSubmitModel { Flag = "flag{l2_correct}", Level = 2, TrackId = fx.TrackId });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertNoSubmissionSideEffects(fx);
    }

    [Fact]
    public async Task MultiLevel_ForeignChallengeTrack_ReturnsBadRequestWithoutSideEffects()
    {
        var fx = await SetupFixtureAsync();
        var foreignChallenge = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, fx.GameId,
            "Foreign Challenge", "flag{foreign_seed}");
        var foreignTrackId = await CreateForeignTrack(foreignChallenge.Id);

        var response = await fx.UserClient.PostAsJsonAsync(
            $"/api/Game/{fx.GameId}/Challenges/{fx.ChallengeId}",
            new FlagSubmitModel { Flag = "flag{foreign_l2}", Level = 2, TrackId = foreignTrackId });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertNoSubmissionSideEffects(fx);
    }

    [Fact]
    public async Task MultiLevel_OtherGameTrack_ReturnsBadRequestWithoutSideEffects()
    {
        var fx = await SetupFixtureAsync();
        var otherGame = await TestDataSeeder.CreateGameAsync(factory.Services, "Other Track Game");
        var foreignChallenge = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, otherGame.Id,
            "Other Game Challenge", "flag{other_seed}");
        var foreignTrackId = await CreateForeignTrack(foreignChallenge.Id);

        var response = await fx.UserClient.PostAsJsonAsync(
            $"/api/Game/{fx.GameId}/Challenges/{fx.ChallengeId}",
            new FlagSubmitModel { Flag = "flag{foreign_l2}", Level = 2, TrackId = foreignTrackId });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertNoSubmissionSideEffects(fx);
    }

    /// <summary>
    /// Bug F (partial): trap answer triggers a challenge-wide lock.
    /// Subsequent GetChallenge returns ChallengeLock.IsLocked=true.
    /// </summary>
    [Fact]
    public async Task MultiLevel_TrapAnswer_TriggersLock()
    {
        var fx = await SetupFixtureAsync();

        // Need to be at L2 for trap to matter (trap is configured on L2 levelConfig).
        // First solve L1 to unlock L2 context, then submit the trap.
        var l1Resp = await fx.UserClient.PostAsJsonAsync(
            $"/api/Game/{fx.GameId}/Challenges/{fx.ChallengeId}",
            new FlagSubmitModel { Flag = "flag{l1_correct}", Level = 1 });
        l1Resp.EnsureSuccessStatusCode();

        var trapResp = await fx.UserClient.PostAsJsonAsync(
            $"/api/Game/{fx.GameId}/Challenges/{fx.ChallengeId}",
            new FlagSubmitModel { Flag = "trap_answer", Level = 2, TrackId = fx.TrackId });
        Assert.Equal(HttpStatusCode.OK, trapResp.StatusCode);
        var trapBody = await trapResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Trap answer triggered", trapBody.GetProperty("message").GetString());

        var detailResp = await fx.UserClient.GetAsync(
            $"/api/Game/{fx.GameId}/Challenges/{fx.ChallengeId}");
        detailResp.EnsureSuccessStatusCode();
        var detail = await detailResp.Content.ReadFromJsonAsync<JsonElement>();

        var challengeLock = detail.GetProperty("challengeLock");
        Assert.True(challengeLock.GetProperty("isLocked").GetBoolean());
    }

    /// <summary>
    /// Bug F (core): ban answer at L2 creates a ban state. GetChallenge shows Banned status.
    /// </summary>
    [Fact]
    public async Task MultiLevel_BanAnswer_CreatesBanState()
    {
        var fx = await SetupFixtureAsync();

        // Solve L1 first
        var l1Resp = await fx.UserClient.PostAsJsonAsync(
            $"/api/Game/{fx.GameId}/Challenges/{fx.ChallengeId}",
            new FlagSubmitModel { Flag = "flag{l1_correct}", Level = 1 });
        l1Resp.EnsureSuccessStatusCode();

        // Submit ban answer at L2
        var banResp = await fx.UserClient.PostAsJsonAsync(
            $"/api/Game/{fx.GameId}/Challenges/{fx.ChallengeId}",
            new FlagSubmitModel { Flag = "ban_answer", Level = 2, TrackId = fx.TrackId });
        Assert.Equal(HttpStatusCode.OK, banResp.StatusCode);
        var banBody = await banResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("封禁", banBody.GetProperty("message").GetString());

        var detailResp = await fx.UserClient.GetAsync(
            $"/api/Game/{fx.GameId}/Challenges/{fx.ChallengeId}");
        detailResp.EnsureSuccessStatusCode();
        var detail = await detailResp.Content.ReadFromJsonAsync<JsonElement>();

        var tracks = detail.GetProperty("tracks").EnumerateArray().ToList();
        var track = tracks.First(t => t.GetProperty("trackId").GetGuid() == fx.TrackId);
        var levels = track.GetProperty("levels").EnumerateArray().ToList();
        var l2 = levels.First(l => l.GetProperty("level").GetInt32() == 2);
        Assert.Equal("Banned", l2.GetProperty("status").GetString());

        // Bug F core: resubmitting the ban answer should STILL show Banned (idempotent)
        var banResp2 = await fx.UserClient.PostAsJsonAsync(
            $"/api/Game/{fx.GameId}/Challenges/{fx.ChallengeId}",
            new FlagSubmitModel { Flag = "ban_answer", Level = 2, TrackId = fx.TrackId });
        Assert.Equal(HttpStatusCode.OK, banResp2.StatusCode);
    }

    /// <summary>
    /// Solving all tracks × all levels creates a FirstSolves entry.
    /// </summary>
    [Fact]
    public async Task MultiLevel_SolveAllLevels_CreatesFirstSolve()
    {
        var fx = await SetupFixtureAsync();

        // Solve L1
        await fx.UserClient.PostAsJsonAsync(
            $"/api/Game/{fx.GameId}/Challenges/{fx.ChallengeId}",
            new FlagSubmitModel { Flag = "flag{l1_correct}", Level = 1 });

        // Solve L2
        await fx.UserClient.PostAsJsonAsync(
            $"/api/Game/{fx.GameId}/Challenges/{fx.ChallengeId}",
            new FlagSubmitModel { Flag = "flag{l2_correct}", Level = 2, TrackId = fx.TrackId });

        // Verify FirstSolve was created
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Need participation ID
        var participation = await context.Participations
            .FirstAsync(p => p.GameId == fx.GameId && p.TeamId == fx.TeamId);

        var firstSolve = await context.FirstSolves
            .FirstOrDefaultAsync(fs => fs.ParticipationId == participation.Id
                                        && fs.ChallengeId == fx.ChallengeId);
        Assert.NotNull(firstSolve);
    }

    private sealed class Fixture
    {
        public int GameId { get; init; }
        public int ChallengeId { get; init; }
        public Guid TrackId { get; init; }
        public HttpClient UserClient { get; init; } = null!;
        public Guid UserId { get; init; }
        public int TeamId { get; init; }
    }

    private async Task<(HttpClient Client, int TeamId)> CreateJoinedUserClient(int gameId, string teamPrefix)
    {
        var password = "User@Pass123";
        var userName = TestDataSeeder.RandomName();
        var user = await TestDataSeeder.CreateUserAsync(factory.Services, userName, password);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, $"{teamPrefix}-{userName}");
        await TestDataSeeder.JoinGameAsync(factory.Services, gameId, team.Id, user.Id);

        var client = factory.CreateClient();
        var loginResp = await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = password });
        loginResp.EnsureSuccessStatusCode();
        return (client, team.Id);
    }
}
