using System.Net;
using System.Net.Http.Json;
using GZCTF.Controllers;
using GZCTF.Integration.Test.Base;
using GZCTF.Models;
using GZCTF.Models.Data;
using GZCTF.Models.Request.Account;
using GZCTF.Models.Request.Game;
using GZCTF.Repositories.Interface;
using GZCTF.Services.Scoring;
using GZCTF.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GZCTF.Integration.Test.Tests.Api;

[Collection(nameof(IntegrationTestCollection))]
public class HybridLineScoringTests(GZCTFApplicationFactory factory)
{
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
        var solvedFlag = Assert.Single(detail.Flags.Where(f => f.IsSolved));
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
        var detail = await response.Content.ReadFromJsonAsync<ChallengeDetailModel>();
        Assert.NotNull(detail);
        return detail!;
    }

    private static async Task<int> GetTeamScore(HttpClient client, int gameId, int teamId)
    {
        var response = await client.GetAsync($"/api/Game/{gameId}/Scoreboard");
        response.EnsureSuccessStatusCode();
        var scoreboard = await response.Content.ReadFromJsonAsync<ScoreboardModel>();
        Assert.NotNull(scoreboard);
        Assert.True(scoreboard!.Items.TryGetValue(teamId, out var item), $"Team {teamId} should be on scoreboard");
        return item.Score;
    }
}
