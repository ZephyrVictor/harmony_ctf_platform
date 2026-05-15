using System.Net;
using System.Net.Http.Json;
using GZCTF.Controllers;
using GZCTF.Integration.Test.Base;
using GZCTF.Models;
using GZCTF.Models.Data;
using GZCTF.Models.Request.Account;
using GZCTF.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace GZCTF.Integration.Test.Tests.Api;

[Collection(nameof(IntegrationTestCollection))]
public class ExtSubmissionTicketTests(GZCTFApplicationFactory factory, ITestOutputHelper output)
{
    [Fact]
    public async Task ExtSubmissionAndTicketLists_FilterByGameId()
    {
        const string password = "ExtGame!123";
        var user = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), password);
        var admin = await TestDataSeeder.CreateUserAsync(
            factory.Services, TestDataSeeder.RandomName(), password, role: Role.Admin);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, TestDataSeeder.RandomName());

        var gameA = await TestDataSeeder.CreateGameAsync(factory.Services, $"Ext Game A {TestDataSeeder.RandomName()}");
        var gameB = await TestDataSeeder.CreateGameAsync(factory.Services, $"Ext Game B {TestDataSeeder.RandomName()}");
        var challengeA = await TestDataSeeder.CreateStaticChallengeAsync(
            factory.Services, gameA.Id, $"Ext Challenge A {TestDataSeeder.RandomName()}", "flag{a}");
        var challengeB = await TestDataSeeder.CreateStaticChallengeAsync(
            factory.Services, gameB.Id, $"Ext Challenge B {TestDataSeeder.RandomName()}", "flag{b}");
        var participationA = await TestDataSeeder.JoinGameAsync(factory.Services, gameA.Id, team.Id, user.Id);
        var participationB = await TestDataSeeder.JoinGameAsync(factory.Services, gameB.Id, team.Id, user.Id);

        await CreateSubmissionRecordAsync(
            gameA.Id, challengeA.Id, team.Id, user.Id, participationA.Id, "wrong-a", -2, true, createTicket: true);
        await CreateSubmissionRecordAsync(
            gameB.Id, challengeB.Id, team.Id, user.Id, participationB.Id, "wrong-b", -2, true, createTicket: true);

        using var userClient = factory.CreateClient();
        var loginResponse = await userClient.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = password });
        loginResponse.EnsureSuccessStatusCode();

        var submissionResponse = await userClient.GetAsync($"/api/ext/submissions/mine?gameId={gameA.Id}");
        submissionResponse.EnsureSuccessStatusCode();
        var submissions = await submissionResponse.Content.ReadFromJsonAsync<SubmissionRecordDto[]>();
        var submission = Assert.Single(submissions!);
        Assert.Equal(gameA.Id, submission.GameId);
        Assert.Equal(challengeA.Id, submission.ChallengeId);

        var userTicketResponse = await userClient.GetAsync($"/api/ext/tickets/mine?gameId={gameA.Id}");
        userTicketResponse.EnsureSuccessStatusCode();
        var userTickets = await userTicketResponse.Content.ReadFromJsonAsync<TicketDto[]>();
        var userTicket = Assert.Single(userTickets!);
        Assert.Equal(gameA.Id, userTicket.GameId);

        using var adminClient = factory.CreateClient();
        var adminLoginResponse = await adminClient.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = admin.UserName, Password = password });
        adminLoginResponse.EnsureSuccessStatusCode();

        var adminTicketResponse = await adminClient.GetAsync($"/api/ext/tickets?gameId={gameA.Id}");
        adminTicketResponse.EnsureSuccessStatusCode();
        var adminTickets = await adminTicketResponse.Content.ReadFromJsonAsync<TicketDto[]>();
        var adminTicket = Assert.Single(adminTickets!);
        Assert.Equal(gameA.Id, adminTicket.GameId);

        output.WriteLine($"Verified ext submission/ticket gameId isolation for game {gameA.Id}");
    }

    [Fact]
    public async Task CreateTicket_RejectsNonPenaltySubmissionRecord()
    {
        const string password = "ExtPenalty!123";
        var user = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), password);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, TestDataSeeder.RandomName());

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, $"Ext Penalty Game {TestDataSeeder.RandomName()}");
        var challenge = await TestDataSeeder.CreateStaticChallengeAsync(
            factory.Services, game.Id, $"Ext Penalty Challenge {TestDataSeeder.RandomName()}", "flag{ok}");
        var participation = await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, team.Id, user.Id);

        var submissionId = await CreateSubmissionRecordAsync(
            game.Id, challenge.Id, team.Id, user.Id, participation.Id, "no-penalty", 0, false);

        using var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = password });
        loginResponse.EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync("/api/ext/tickets",
            new TicketCreateModel { SubmissionId = submissionId, Description = "This should not be appealable" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<int> CreateSubmissionRecordAsync(
        int gameId,
        int challengeId,
        int teamId,
        Guid userId,
        int participationId,
        string answer,
        int scoreChange,
        bool isPenalty,
        bool createTicket = false)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var submission = new Submission
        {
            GameId = gameId,
            ChallengeId = challengeId,
            ParticipationId = participationId,
            TeamId = teamId,
            UserId = userId,
            Answer = answer,
            Status = isPenalty ? AnswerResult.WrongAnswer : AnswerResult.Accepted,
            SubmitTimeUtc = DateTimeOffset.UtcNow
        };

        await context.Submissions.AddAsync(submission);
        await context.SaveChangesAsync();

        await context.SubmissionRecords.AddAsync(new SubmissionRecord
        {
            SubmissionId = submission.Id,
            ScoreChange = scoreChange,
            IsPenalty = isPenalty
        });

        if (createTicket)
        {
            await context.Tickets.AddAsync(new Ticket
            {
                SubmissionId = submission.Id,
                UserId = userId,
                Description = $"Appeal for {answer}"
            });
        }

        await context.SaveChangesAsync();
        return submission.Id;
    }
}
