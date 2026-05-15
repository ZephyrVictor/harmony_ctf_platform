using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using GZCTF.Controllers;
using GZCTF.Integration.Test.Base;
using GZCTF.Models;
using GZCTF.Models.Data;
using GZCTF.Models.Request.Account;
using GZCTF.Models.Request.Edit;
using GZCTF.Models.Request.Game;
using GZCTF.Repositories.Interface;
using GZCTF.Services.Scoring;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace GZCTF.Integration.Test.Tests.Api;

[Collection(nameof(IntegrationTestCollection))]
public class HybridLineScoringTests(GZCTFApplicationFactory factory)
{
    static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new DateTimeOffsetJsonConverter() }
    };

    [Fact]
    public async Task HybridLine_SubmissionsScorePerFlagWithoutChallengeBonus()
    {
        const string password = "HybridLine!123";
        var user = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), password);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, TestDataSeeder.RandomName());
        var game = await TestDataSeeder.CreateGameAsync(factory.Services, $"HybridLine Game {TestDataSeeder.RandomName()}");
        var challengeId = await CreateHybridLineChallengeAsync(game.Id);

        await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, team.Id, user.Id);

        using var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = password });
        loginResponse.EnsureSuccessStatusCode();

        await Submit(client, game.Id, challengeId, "166");
        Assert.Equal(4, await GetTeamScore(client, game.Id, team.Id));
        var detail = await GetChallengeDetail(client, game.Id, challengeId);
        var solvedFlag = Assert.Single(detail.Flags, f => f.IsSolved);
        Assert.Equal("Flaw1", solvedFlag.FlawNo);

        await Submit(client, game.Id, challengeId, "c:348-350");
        Assert.Equal(8, await GetTeamScore(client, game.Id, team.Id));

        await Submit(client, game.Id, challengeId, "c:281、c:286");
        Assert.Equal(12, await GetTeamScore(client, game.Id, team.Id));

        await Submit(client, game.Id, challengeId, "c:166");
        Assert.Equal(12, await GetTeamScore(client, game.Id, team.Id));

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Empty(context.FirstSolves.Where(fs => fs.ChallengeId == challengeId));
        Assert.Equal(3, context.SubmissionRecords.Count(r =>
            r.ChallengeId == challengeId
            && r.MatchedFlagId != null
            && r.ScoreChange == 4));
    }

    [Fact]
    public async Task HybridLine_PositiveRecordDoesNotScoreWithoutDivisionGetScore()
    {
        const string password = "HybridLineNoScore!123";
        var user = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), password);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, TestDataSeeder.RandomName());
        var game = await TestDataSeeder.CreateGameAsync(factory.Services, $"HybridLine NoScore {TestDataSeeder.RandomName()}");
        var challengeId = await CreateHybridLineChallengeAsync(game.Id);
        var division = await CreateDivision(game.Id, new DivisionCreateModel
        {
            Name = $"NoScore {TestDataSeeder.RandomName()}",
            DefaultPermissions = GamePermission.All & ~GamePermission.RequireReview,
            ChallengeConfigs =
            [
                new()
                {
                    ChallengeId = challengeId,
                    Permissions = GamePermission.ViewChallenge | GamePermission.SubmitFlags | GamePermission.RankOverall
                }
            ]
        });

        await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, team.Id, user.Id, division.Id);

        using var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = password });
        loginResponse.EnsureSuccessStatusCode();

        await Submit(client, game.Id, challengeId, "166");

        Assert.Equal(0, await GetTeamScore(client, game.Id, team.Id));

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, context.SubmissionRecords.Count(r =>
            r.ChallengeId == challengeId
            && r.MatchedFlagId != null
            && r.ScoreChange == 4));
    }

    [Fact]
    public async Task HybridLine_PositiveRecordsRespectGameWindowAndChallengeDeadline()
    {
        const string password = "HybridLineWindow!123";
        var now = DateTimeOffset.UtcNow;
        var endedGame = await TestDataSeeder.CreateGameAsync(factory.Services,
            $"HybridLine Ended {TestDataSeeder.RandomName()}",
            start: now.AddHours(-3),
            end: now.AddHours(-2),
            practiceMode: true);
        var endedChallengeId = await CreateHybridLineChallengeAsync(endedGame.Id);
        var endedUser = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), password);
        var endedTeam = await TestDataSeeder.CreateTeamAsync(factory.Services, endedUser.Id, TestDataSeeder.RandomName());
        var endedParticipation =
            await TestDataSeeder.JoinGameAsync(factory.Services, endedGame.Id, endedTeam.Id, endedUser.Id);

        await CreateHybridLineRecord(
            endedGame.Id,
            endedChallengeId,
            endedParticipation.Id,
            endedTeam.Id,
            endedUser.Id,
            "c:166",
            now);

        Assert.Equal(0, await GetTeamScore(endedGame.Id, endedTeam.Id));

        var deadlineGame = await TestDataSeeder.CreateGameAsync(factory.Services,
            $"HybridLine Deadline {TestDataSeeder.RandomName()}",
            start: now.AddHours(-1),
            end: now.AddHours(1),
            practiceMode: true);
        var deadlineChallengeId = await CreateHybridLineChallengeAsync(deadlineGame.Id);
        await SetChallengeDeadline(deadlineChallengeId, now.AddMinutes(-10));
        var deadlineUser = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), password);
        var deadlineTeam =
            await TestDataSeeder.CreateTeamAsync(factory.Services, deadlineUser.Id, TestDataSeeder.RandomName());
        var deadlineParticipation =
            await TestDataSeeder.JoinGameAsync(factory.Services, deadlineGame.Id, deadlineTeam.Id, deadlineUser.Id);

        await CreateHybridLineRecord(
            deadlineGame.Id,
            deadlineChallengeId,
            deadlineParticipation.Id,
            deadlineTeam.Id,
            deadlineUser.Id,
            "c:166",
            now);

        Assert.Equal(0, await GetTeamScore(deadlineGame.Id, deadlineTeam.Id));
    }

    [Fact]
    public async Task HybridLine_MatchesEnabledFlagsButOnlyDisplaysVisibleEnabledFlags()
    {
        const string password = "HybridLineVisibility!123";
        var user = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), password);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, TestDataSeeder.RandomName());
        var game =
            await TestDataSeeder.CreateGameAsync(factory.Services, $"HybridLine Visibility {TestDataSeeder.RandomName()}");
        var challengeId = await CreateHybridLineChallengeAsync(game.Id);

        await SetFlagState(challengeId, "Flaw1", isVisible: false, isEnabled: true);
        await SetFlagState(challengeId, "Flaw9", isVisible: true, isEnabled: false);
        await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, team.Id, user.Id);

        using var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = password });
        loginResponse.EnsureSuccessStatusCode();

        var detail = await GetChallengeDetail(client, game.Id, challengeId);
        Assert.DoesNotContain(detail.Flags, f => f.FlawNo == "Flaw1");
        Assert.DoesNotContain(detail.Flags, f => f.FlawNo == "Flaw9");
        Assert.Contains(detail.Flags, f => f.FlawNo == "Flaw4");

        await Submit(client, game.Id, challengeId, "166");
        Assert.Equal(4, await GetTeamScore(client, game.Id, team.Id));
    }

    [Fact]
    public async Task HybridLine_ConcurrentDuplicateSubmissionIsIdempotent()
    {
        const string password = "HybridLineRace!123";
        var user = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), password);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, TestDataSeeder.RandomName());
        var game = await TestDataSeeder.CreateGameAsync(factory.Services, $"HybridLine Race {TestDataSeeder.RandomName()}");
        var challengeId = await CreateHybridLineChallengeAsync(game.Id);

        await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, team.Id, user.Id);

        using var clientA = factory.CreateClient();
        using var clientB = factory.CreateClient();
        foreach (var client in new[] { clientA, clientB })
        {
            var loginResponse = await client.PostAsJsonAsync("/api/Account/LogIn",
                new LoginModel { UserName = user.UserName, Password = password });
            loginResponse.EnsureSuccessStatusCode();
        }

        var responses = await Task.WhenAll(
            clientA.PostAsJsonAsync($"/api/Game/{game.Id}/Challenges/{challengeId}",
                new FlagSubmitModel { Flag = "166" }),
            clientB.PostAsJsonAsync($"/api/Game/{game.Id}/Challenges/{challengeId}",
                new FlagSubmitModel { Flag = "c:166" }));

        Assert.All(responses, r => r.EnsureSuccessStatusCode());
        Assert.Equal(4, await GetTeamScore(clientA, game.Id, team.Id));

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, context.SubmissionRecords.Count(r =>
            r.ChallengeId == challengeId
            && r.MatchedFlagId != null
            && r.ScoreChange == 4));
    }

    [Fact]
    public async Task HybridLine_InvalidSubmissionFormatReturnsBadRequest()
    {
        const string password = "HybridLineBad!123";
        var user = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), password);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, TestDataSeeder.RandomName());
        var game = await TestDataSeeder.CreateGameAsync(factory.Services, $"HybridLine Bad {TestDataSeeder.RandomName()}");
        var challengeId = await CreateHybridLineChallengeAsync(game.Id);

        await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, team.Id, user.Id);

        using var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = password });
        loginResponse.EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync($"/api/Game/{game.Id}/Challenges/{challengeId}",
            new FlagSubmitModel { Flag = "line-one" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task HybridLine_AdminRejectsInvalidFlagAnswer()
    {
        const string password = "HybridLineAdmin!123";
        var admin = await TestDataSeeder.CreateUserAsync(
            factory.Services, TestDataSeeder.RandomName(), password, role: Role.Admin);
        var game = await TestDataSeeder.CreateGameAsync(factory.Services, $"HybridLine Admin {TestDataSeeder.RandomName()}");
        var challengeId = await CreateHybridLineChallengeAsync(game.Id);

        using var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = admin.UserName, Password = password });
        loginResponse.EnsureSuccessStatusCode();

        var createResponse = await client.PostAsJsonAsync($"/api/ext/challenges/{challengeId}/flags",
            new ChallengeFlagCreateModel
            {
                FlagValue = "not-a-line",
                RawAnswer = "not-a-line",
                AnswerType = ChallengeFlagAnswerType.SingleLine,
                Score = 4
            });
        Assert.Equal(HttpStatusCode.BadRequest, createResponse.StatusCode);

        Guid flagId;
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            flagId = context.ChallengeFlags
                .Where(f => f.ChallengeId == challengeId)
                .Select(f => f.Id)
                .First();
        }

        var updateResponse = await client.PutAsJsonAsync($"/api/ext/challenges/{challengeId}/flags/{flagId}",
            new ChallengeFlagUpdateModel
            {
                RawAnswer = "300-200",
                AnswerType = ChallengeFlagAnswerType.LineRange
            });
        Assert.Equal(HttpStatusCode.BadRequest, updateResponse.StatusCode);
    }

    [Fact]
    public void HybridLine_PositiveFlagIdempotencyOnlyHandlesTargetUniqueConstraint()
    {
        ScoreResult positiveFlagHit = new()
        {
            MatchedFlagId = Guid.NewGuid(),
            ScoreChange = 4
        };

        Assert.True(IsPositiveFlagDuplicateConstraintViolation(
            positiveFlagHit, CreateUniqueViolation("IX_SubmissionRecords_ParticipationId_MatchedFlagId")));
        Assert.False(IsPositiveFlagDuplicateConstraintViolation(
            positiveFlagHit, CreateUniqueViolation("PK_FirstSolves")));
        Assert.False(IsPositiveFlagDuplicateConstraintViolation(
            new ScoreResult { MatchedFlagId = positiveFlagHit.MatchedFlagId, ScoreChange = 0 },
            CreateUniqueViolation("IX_SubmissionRecords_ParticipationId_MatchedFlagId")));
    }

    private async Task<int> CreateHybridLineChallengeAsync(int gameId)
    {
        using var scope = factory.Services.CreateScope();
        var gameRepository = scope.ServiceProvider.GetRequiredService<IGameRepository>();
        var challengeRepository = scope.ServiceProvider.GetRequiredService<IGameChallengeRepository>();

        var game = await gameRepository.GetGameById(gameId)
                   ?? throw new InvalidOperationException($"Game {gameId} not found");

        GameChallenge challenge = new()
        {
            Title = $"HybridLine Challenge {TestDataSeeder.RandomName()}",
            Content = "HybridLine subsystem content",
            Category = ChallengeCategory.Misc,
            Type = ChallengeType.StaticAttachment,
            Hints = [],
            IsEnabled = true,
            SubmissionLimit = 0,
            OriginalScore = 100,
            MinScoreRate = 0.8,
            Difficulty = 5,
            FlagMatchMode = FlagMatchMode.HybridLine,
            Game = game,
            GameId = game.Id
        };

        challenge.Flags.Add(CreateHybridFlag("c:166", ChallengeFlagAnswerType.SingleLine, "Flaw1"));
        challenge.Flags.Add(CreateHybridFlag("c:347-352", ChallengeFlagAnswerType.LineRange, "Flaw9"));
        challenge.Flags.Add(CreateHybridFlag("c:281、c:286", ChallengeFlagAnswerType.MultiLine, "Flaw4"));

        await challengeRepository.CreateChallenge(game, challenge);
        return challenge.Id;
    }

    private async Task<Division> CreateDivision(int gameId, DivisionCreateModel model)
    {
        using var scope = factory.Services.CreateScope();
        var gameRepository = scope.ServiceProvider.GetRequiredService<IGameRepository>();
        var divisionRepository = scope.ServiceProvider.GetRequiredService<IDivisionRepository>();
        var game = await gameRepository.GetGameById(gameId)
                   ?? throw new InvalidOperationException($"Game {gameId} not found");

        return await divisionRepository.CreateDivision(game, model, CancellationToken.None);
    }

    private async Task SetChallengeDeadline(int challengeId, DateTimeOffset deadline)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var challenge = await context.GameChallenges.FirstAsync(c => c.Id == challengeId);
        challenge.DeadlineUtc = deadline;
        await context.SaveChangesAsync();
    }

    private async Task SetFlagState(int challengeId, string flawNo, bool isVisible, bool isEnabled)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var flag = await context.ChallengeFlags.FirstAsync(f => f.ChallengeId == challengeId && f.FlawNo == flawNo);
        flag.IsVisible = isVisible;
        flag.IsEnabled = isEnabled;
        await context.SaveChangesAsync();
    }

    private async Task CreateHybridLineRecord(int gameId, int challengeId, int participationId, int teamId,
        Guid userId, string answer, DateTimeOffset submitTime)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var flagId = await context.ChallengeFlags
            .Where(f => f.ChallengeId == challengeId && f.RawAnswer == answer)
            .Select(f => f.Id)
            .FirstAsync();

        Submission submission = new()
        {
            Answer = answer,
            Status = AnswerResult.Accepted,
            UserId = userId,
            TeamId = teamId,
            ParticipationId = participationId,
            GameId = gameId,
            ChallengeId = challengeId,
            SubmitTimeUtc = submitTime
        };

        await context.Submissions.AddAsync(submission);
        await context.SaveChangesAsync();

        await context.SubmissionRecords.AddAsync(new SubmissionRecord
        {
            SubmissionId = submission.Id,
            ScoreChange = 4,
            IsPenalty = false,
            MatchedFlagId = flagId,
            GameId = gameId,
            ChallengeId = challengeId,
            ParticipationId = participationId
        });
        await context.SaveChangesAsync();
    }

    private static ChallengeFlag CreateHybridFlag(string answer, ChallengeFlagAnswerType answerType, string flawNo) =>
        new()
        {
            FlagValue = answer,
            RawAnswer = answer,
            NormalizedAnswerKey = ScoringService.NormalizeHybridLineAnswer(answer, answerType),
            AnswerType = answerType,
            FlawNo = flawNo,
            FilePath = "ecmascript/example.cpp",
            Cwe = "CWE-787",
            Score = 4,
            IsVisible = true,
            IsEnabled = true
        };

    private static async Task Submit(HttpClient client, int gameId, int challengeId, string answer)
    {
        var response = await client.PostAsJsonAsync($"/api/Game/{gameId}/Challenges/{challengeId}",
            new FlagSubmitModel { Flag = answer });
        response.EnsureSuccessStatusCode();
    }

    private static async Task<ChallengeDetailModel> GetChallengeDetail(HttpClient client, int gameId, int challengeId)
    {
        var response = await client.GetAsync($"/api/Game/{gameId}/Challenges/{challengeId}");
        response.EnsureSuccessStatusCode();
        var detail = await response.Content.ReadFromJsonAsync<ChallengeDetailModel>(JsonOptions);
        Assert.NotNull(detail);
        return detail!;
    }

    private static async Task<int> GetTeamScore(HttpClient client, int gameId, int teamId)
    {
        var response = await client.GetAsync($"/api/Game/{gameId}/Scoreboard");
        response.EnsureSuccessStatusCode();
        var scoreboard = await response.Content.ReadFromJsonAsync<ScoreboardSnapshot>(JsonOptions);
        Assert.NotNull(scoreboard);
        var item = Assert.Single(scoreboard!.Items, i => i.Id == teamId);
        return item.Score;
    }

    private async Task<int> GetTeamScore(int gameId, int teamId)
    {
        using var scope = factory.Services.CreateScope();
        var gameRepository = scope.ServiceProvider.GetRequiredService<IGameRepository>();
        var game = await gameRepository.GetGameById(gameId)
                   ?? throw new InvalidOperationException($"Game {gameId} not found");
        var scoreboard = await gameRepository.GetScoreboard(game);
        var item = scoreboard.Items[teamId];
        return item.Score;
    }

    private sealed record ScoreboardSnapshot(List<ScoreboardItem> Items);

    private static bool IsPositiveFlagDuplicateConstraintViolation(ScoreResult result, DbUpdateException exception)
    {
        var method = typeof(GameController).GetMethod(
            "IsPositiveFlagDuplicateConstraintViolation",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);

        return (bool)method!.Invoke(null, [result, exception])!;
    }

    private static DbUpdateException CreateUniqueViolation(string constraintName) =>
        new("database update failed",
            new PostgresException("duplicate key value violates unique constraint", "ERROR", "ERROR",
                PostgresErrorCodes.UniqueViolation, constraintName: constraintName));
}
