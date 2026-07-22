using System.Net.Http.Json;
using System.Text.Json;
using GZCTF.Integration.Test.Base;
using GZCTF.Models;
using GZCTF.Models.Data;
using GZCTF.Models.Request.Account;
using GZCTF.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GZCTF.Integration.Test.Tests.Api;

[Collection(nameof(IntegrationTestCollection))]
public class TeamProgressTests(GZCTFApplicationFactory factory)
{
    [Fact]
    public async Task TeamProgress_ShouldSummarizeSubmissionsLevelsAndZeroDayBonuses()
    {
        const string adminPassword = "Admin@Progress123";
        var admin = await TestDataSeeder.CreateUserAsync(
            factory.Services,
            TestDataSeeder.RandomName(),
            adminPassword,
            role: Role.Admin);

        var user = await TestDataSeeder.CreateUserAsync(
            factory.Services,
            TestDataSeeder.RandomName(),
            "User@Progress123");
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, $"Progress Team {TestDataSeeder.RandomName()}");
        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "Team Progress Game");
        var challenge = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id, "Progress Challenge", "flag{seed}");

        int participationId;
        Guid trackId;
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var participation = await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, team.Id, user.Id);
            participationId = participation.Id;

            var track = new ChallengeTrack
            {
                Id = Guid.NewGuid(),
                ChallengeId = challenge.Id,
                Title = "Progress Track",
                Repository = "repo",
                Module = "module",
                FilePath = "src/progress.cpp:10",
                IsEnabled = true
            };
            trackId = track.Id;
            context.ChallengeTracks.Add(track);
            context.ChallengeLevelConfigs.AddRange(
                new ChallengeLevelConfig { ChallengeId = challenge.Id, Level = 1, Title = "Locate", Score = 5, IsEnabled = true },
                new ChallengeLevelConfig { ChallengeId = challenge.Id, Level = 2, Title = "Commit", Score = 4, IsEnabled = true });

            var wrongSubmission = new Submission
            {
                GameId = game.Id,
                ChallengeId = challenge.Id,
                ParticipationId = participationId,
                TeamId = team.Id,
                UserId = user.Id,
                Answer = "wrong-answer",
                Status = AnswerResult.WrongAnswer,
                SubmitTimeUtc = DateTimeOffset.UtcNow.AddMinutes(-3)
            };
            var acceptedSubmission = new Submission
            {
                GameId = game.Id,
                ChallengeId = challenge.Id,
                ParticipationId = participationId,
                TeamId = team.Id,
                UserId = user.Id,
                Answer = "flag{level1}",
                Status = AnswerResult.Accepted,
                SubmitTimeUtc = DateTimeOffset.UtcNow.AddMinutes(-2)
            };

            context.Submissions.AddRange(wrongSubmission, acceptedSubmission);
            await context.SaveChangesAsync();

            context.SubmissionRecords.AddRange(
                new SubmissionRecord
                {
                    SubmissionId = wrongSubmission.Id,
                    ScoreChange = -1,
                    IsPenalty = true,
                    GameId = game.Id,
                    ChallengeId = challenge.Id,
                    ParticipationId = participationId,
                    TrackId = trackId,
                    Level = 1,
                    SubmissionType = "Wrong"
                },
                new SubmissionRecord
                {
                    SubmissionId = acceptedSubmission.Id,
                    ScoreChange = 5,
                    IsPenalty = false,
                    GameId = game.Id,
                    ChallengeId = challenge.Id,
                    ParticipationId = participationId,
                    TrackId = trackId,
                    Level = 1,
                    SubmissionType = "Correct"
                });
            context.ScoreAdjustments.Add(new ScoreAdjustment
            {
                GameId = game.Id,
                TeamId = team.Id,
                Delta = 100,
                Reason = "0day progress test",
                Category = ScoreAdjustmentCategory.ZeroDayBonus,
                ZeroDaySeverity = ZeroDaySeverity.Critical,
                OperatorId = admin.Id,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await context.SaveChangesAsync();
        }

        using var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = admin.UserName, Password = adminPassword });
        login.EnsureSuccessStatusCode();

        var listResponse = await client.GetAsync($"/api/admin/games/{game.Id}/team-progress?q={Uri.EscapeDataString(team.Name)}");
        listResponse.EnsureSuccessStatusCode();
        var list = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        var summary = list.GetProperty("data").EnumerateArray().Single();
        Assert.Equal(team.Id, summary.GetProperty("teamId").GetInt32());
        Assert.Equal(2, summary.GetProperty("totalSubmissions").GetInt32());
        Assert.Equal(1, summary.GetProperty("acceptedSubmissions").GetInt32());
        Assert.Equal(1, summary.GetProperty("wrongSubmissions").GetInt32());
        Assert.Equal(1, summary.GetProperty("penaltySubmissions").GetInt32());
        Assert.Equal(1, summary.GetProperty("solvedLevels").GetInt32());
        Assert.Equal(2, summary.GetProperty("totalLevels").GetInt32());
        Assert.Equal(0, summary.GetProperty("completedTracks").GetInt32());
        Assert.Equal(1, summary.GetProperty("totalTracks").GetInt32());
        Assert.Equal(1, summary.GetProperty("zeroDayApprovedCount").GetInt32());
        Assert.Equal(100, summary.GetProperty("zeroDayScore").GetInt32());

        var detailResponse = await client.GetAsync($"/api/admin/games/{game.Id}/team-progress/{team.Id}");
        detailResponse.EnsureSuccessStatusCode();
        var detail = await detailResponse.Content.ReadFromJsonAsync<JsonElement>();

        var levels = detail.GetProperty("challenges")[0].GetProperty("tracks")[0].GetProperty("levels").EnumerateArray().ToArray();
        Assert.Equal("Solved", levels.Single(l => l.GetProperty("level").GetInt32() == 1).GetProperty("status").GetString());
        Assert.Equal("Unsolved", levels.Single(l => l.GetProperty("level").GetInt32() == 2).GetProperty("status").GetString());
        Assert.Equal("flag{level1}", levels.Single(l => l.GetProperty("level").GetInt32() == 1).GetProperty("acceptedAnswer").GetString());

        var submissions = detail.GetProperty("submissions").EnumerateArray().ToArray();
        Assert.Equal(2, submissions.Length);
        Assert.Contains(submissions, s => s.GetProperty("answer").GetString() == "wrong-answer");
        Assert.Contains(submissions, s => s.GetProperty("answer").GetString() == "flag{level1}");

        var zeroDayBonuses = detail.GetProperty("zeroDayBonuses").EnumerateArray().ToArray();
        Assert.Single(zeroDayBonuses);
        Assert.Equal("Critical", zeroDayBonuses[0].GetProperty("severity").GetString());
    }
}
