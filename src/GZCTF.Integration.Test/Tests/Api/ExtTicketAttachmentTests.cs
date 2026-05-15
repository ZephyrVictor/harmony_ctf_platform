using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using GZCTF.Controllers;
using GZCTF.Integration.Test.Base;
using GZCTF.Models;
using GZCTF.Models.Data;
using GZCTF.Models.Request.Account;
using GZCTF.Services.Scoring;
using GZCTF.Utils;
using Microsoft.Extensions.DependencyInjection;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace GZCTF.Integration.Test.Tests.Api;

[Collection(nameof(IntegrationTestCollection))]
public class ExtTicketAttachmentTests(GZCTFApplicationFactory factory)
{
    static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new DateTimeOffsetJsonConverter() }
    };

    [Fact]
    public async Task TicketAttachment_Upload_ReturnsInDetailAndLists()
    {
        var fixture = await CreatePenaltySubmissionFixture("Ticket Attachment");
        using var ownerClient = factory.CreateClient();
        await Login(ownerClient, fixture.User);

        var createResponse = await ownerClient.PostAsJsonAsync("/api/ext/tickets", new TicketCreateModel
        {
            SubmissionId = fixture.SubmissionId,
            GameId = fixture.Game.Id,
            Description = "Need review with screenshot"
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<TicketDto>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal(fixture.Game.Id, created.GameId);
        Assert.Empty(created.Attachments);

        using var form = CreateImageForm("evidence.png");
        var uploadResponse = await ownerClient.PostAsync($"/api/ext/tickets/{created.Id}/attachments", form);

        Assert.Equal(HttpStatusCode.Created, uploadResponse.StatusCode);
        var uploaded = await uploadResponse.Content.ReadFromJsonAsync<TicketAttachmentDto[]>(JsonOptions);
        Assert.NotNull(uploaded);
        var attachment = Assert.Single(uploaded);
        Assert.NotEqual("evidence.png", attachment.FileName);
        Assert.EndsWith(".png", attachment.FileName);
        Assert.True(Guid.TryParseExact(Path.GetFileNameWithoutExtension(attachment.FileName), "N", out _));
        Assert.Equal("image/png", attachment.ContentType);
        Assert.NotNull(attachment.Url);

        var detail = await ownerClient.GetFromJsonAsync<TicketDto>($"/api/ext/tickets/{created.Id}", JsonOptions);
        Assert.NotNull(detail);
        var detailAttachment = Assert.Single(detail.Attachments);
        Assert.Equal(attachment.Id, detailAttachment.Id);

        var mine = await ownerClient.GetFromJsonAsync<TicketDto[]>(
            $"/api/ext/tickets/mine?gameId={fixture.Game.Id}", JsonOptions);
        Assert.NotNull(mine);
        var listed = Assert.Single(mine);
        Assert.Single(listed.Attachments);

        var otherGame = await TestDataSeeder.CreateGameAsync(factory.Services, $"Other Game {TestDataSeeder.RandomName()}");
        var emptyMine = await ownerClient.GetFromJsonAsync<TicketDto[]>(
            $"/api/ext/tickets/mine?gameId={otherGame.Id}", JsonOptions);
        Assert.NotNull(emptyMine);
        Assert.Empty(emptyMine);

        var otherUser = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), "Other@123");
        using var otherClient = factory.CreateClient();
        await Login(otherClient, otherUser);

        using var forbiddenForm = CreateImageForm("foreign.png");
        var forbidden = await otherClient.PostAsync($"/api/ext/tickets/{created.Id}/attachments", forbiddenForm);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task TicketAttachment_RejectsSixthAttachment()
    {
        var fixture = await CreatePenaltySubmissionFixture("Ticket Attachment Limit");
        using var ownerClient = factory.CreateClient();
        await Login(ownerClient, fixture.User);
        var ticket = await CreateTicket(ownerClient, fixture);

        using var firstFive = CreateImageForm("one.png", "two.png", "three.png", "four.png", "five.png");
        var firstFiveResponse = await ownerClient.PostAsync($"/api/ext/tickets/{ticket.Id}/attachments", firstFive);
        Assert.Equal(HttpStatusCode.Created, firstFiveResponse.StatusCode);

        using var sixth = CreateImageForm("six.png");
        var sixthResponse = await ownerClient.PostAsync($"/api/ext/tickets/{ticket.Id}/attachments", sixth);
        Assert.Equal(HttpStatusCode.BadRequest, sixthResponse.StatusCode);

        var detail = await ownerClient.GetFromJsonAsync<TicketDto>($"/api/ext/tickets/{ticket.Id}", JsonOptions);
        Assert.NotNull(detail);
        Assert.Equal(5, detail.Attachments.Length);
    }

    [Fact]
    public async Task TicketAttachment_RejectsOversizedPlainTextAndSpoofedImages()
    {
        var fixture = await CreatePenaltySubmissionFixture("Ticket Attachment Validation");
        using var ownerClient = factory.CreateClient();
        await Login(ownerClient, fixture.User);
        var ticket = await CreateTicket(ownerClient, fixture);

        using var oversized = CreateForm(new byte[(10 * 1024 * 1024) + 1], "huge.png", "image/png");
        var oversizedResponse = await ownerClient.PostAsync($"/api/ext/tickets/{ticket.Id}/attachments", oversized);
        Assert.Equal(HttpStatusCode.BadRequest, oversizedResponse.StatusCode);

        using var text = CreateForm(Encoding.UTF8.GetBytes("not an image"), "evidence.txt", "text/plain");
        var textResponse = await ownerClient.PostAsync($"/api/ext/tickets/{ticket.Id}/attachments", text);
        Assert.Equal(HttpStatusCode.BadRequest, textResponse.StatusCode);

        using var spoofed = CreateForm(Encoding.UTF8.GetBytes("<html>not an image</html>"), "spoof.png", "image/png");
        var spoofedResponse = await ownerClient.PostAsync($"/api/ext/tickets/{ticket.Id}/attachments", spoofed);
        Assert.Equal(HttpStatusCode.BadRequest, spoofedResponse.StatusCode);
    }

    [Fact]
    public async Task TicketAttachment_UsesDecodedContentTypeForMismatchedPngHeaders()
    {
        var fixture = await CreatePenaltySubmissionFixture("Ticket Attachment Decoded Content Type");
        using var ownerClient = factory.CreateClient();
        await Login(ownerClient, fixture.User);
        var ticket = await CreateTicket(ownerClient, fixture);

        using var form = new MultipartFormDataContent();
        AddFile(form, PngBytes, "plain.txt", "text/plain");
        AddFile(form, PngBytes, "vector.svg", "image/svg+xml");

        var response = await ownerClient.PostAsync($"/api/ext/tickets/{ticket.Id}/attachments", form);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var uploaded = await response.Content.ReadFromJsonAsync<TicketAttachmentDto[]>(JsonOptions);
        Assert.NotNull(uploaded);
        Assert.Equal(2, uploaded.Length);
        Assert.All(uploaded, attachment =>
        {
            Assert.Equal("image/png", attachment.ContentType);
            Assert.EndsWith(".png", attachment.FileName);
        });
    }

    [Fact]
    public async Task TicketAttachment_RejectsNonPendingTicket()
    {
        var fixture = await CreatePenaltySubmissionFixture("Ticket Attachment Non Pending");
        using var ownerClient = factory.CreateClient();
        await Login(ownerClient, fixture.User);
        var ticket = await CreateTicket(ownerClient, fixture);

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var ticketEntity = await context.Tickets.FindAsync(ticket.Id);
            Assert.NotNull(ticketEntity);
            ticketEntity.Status = TicketStatus.Approved;
            ticketEntity.ReviewerId = fixture.User.Id;
            ticketEntity.ReviewedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync();
        }

        using var form = CreateImageForm("after-review.png");
        var response = await ownerClient.PostAsync($"/api/ext/tickets/{ticket.Id}/attachments", form);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task TicketAttachment_ConcurrentUploadsCannotExceedLimit()
    {
        var fixture = await CreatePenaltySubmissionFixture("Ticket Attachment Concurrent Limit");
        using var ownerClient = factory.CreateClient();
        await Login(ownerClient, fixture.User);
        var ticket = await CreateTicket(ownerClient, fixture);

        using var firstFour = CreateImageForm("one.png", "two.png", "three.png", "four.png");
        var firstFourResponse = await ownerClient.PostAsync($"/api/ext/tickets/{ticket.Id}/attachments", firstFour);
        Assert.Equal(HttpStatusCode.Created, firstFourResponse.StatusCode);

        var responses = await Task.WhenAll(
            UploadImage(ownerClient, ticket.Id, "five-a.png"),
            UploadImage(ownerClient, ticket.Id, "five-b.png"));
        Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.BadRequest));

        foreach (var response in responses)
            response.Dispose();

        var detail = await ownerClient.GetFromJsonAsync<TicketDto>($"/api/ext/tickets/{ticket.Id}", JsonOptions);
        Assert.NotNull(detail);
        Assert.Equal(5, detail.Attachments.Length);
    }

    [Fact]
    public async Task TicketCreate_RejectsNonPenaltyAndGameMismatch()
    {
        var nonPenaltyFixture = await CreatePenaltySubmissionFixture("Non Penalty", isPenalty: false);
        using var client = factory.CreateClient();
        await Login(client, nonPenaltyFixture.User);

        var nonPenaltyResponse = await client.PostAsJsonAsync("/api/ext/tickets", new TicketCreateModel
        {
            SubmissionId = nonPenaltyFixture.SubmissionId,
            GameId = nonPenaltyFixture.Game.Id,
            Description = "This zero point wrong answer should not be appealable"
        });

        Assert.Equal(HttpStatusCode.BadRequest, nonPenaltyResponse.StatusCode);

        var otherUser = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), "Foreign@123");
        using var foreignClient = factory.CreateClient();
        await Login(foreignClient, otherUser);

        var foreignNonPenaltyResponse = await foreignClient.PostAsJsonAsync("/api/ext/tickets", new TicketCreateModel
        {
            SubmissionId = nonPenaltyFixture.SubmissionId,
            GameId = nonPenaltyFixture.Game.Id,
            Description = "Foreign non-penalty submission"
        });

        Assert.Equal(HttpStatusCode.Forbidden, foreignNonPenaltyResponse.StatusCode);

        var penaltyFixture = await CreatePenaltySubmissionFixture("Game Mismatch");
        var otherGame = await TestDataSeeder.CreateGameAsync(factory.Services, $"Mismatch Game {TestDataSeeder.RandomName()}");
        using var ownerClient = factory.CreateClient();
        await Login(ownerClient, penaltyFixture.User);

        var mismatchResponse = await ownerClient.PostAsJsonAsync("/api/ext/tickets", new TicketCreateModel
        {
            SubmissionId = penaltyFixture.SubmissionId,
            GameId = otherGame.Id,
            Description = "Wrong game"
        });

        Assert.Equal(HttpStatusCode.BadRequest, mismatchResponse.StatusCode);

        var ownerTicket = await ownerClient.PostAsJsonAsync("/api/ext/tickets", new TicketCreateModel
        {
            SubmissionId = penaltyFixture.SubmissionId,
            GameId = penaltyFixture.Game.Id,
            Description = "Owner appeal"
        });
        Assert.Equal(HttpStatusCode.Created, ownerTicket.StatusCode);

        var foreignResponse = await foreignClient.PostAsJsonAsync("/api/ext/tickets", new TicketCreateModel
        {
            SubmissionId = penaltyFixture.SubmissionId,
            GameId = penaltyFixture.Game.Id,
            Description = "Foreign submission"
        });

        Assert.Equal(HttpStatusCode.Forbidden, foreignResponse.StatusCode);
    }

    [Fact]
    public async Task TicketDefaults_EnableOneAppealWhenSettingRowIsMissing()
    {
        using var scope = factory.Services.CreateScope();
        var settings = scope.ServiceProvider.GetRequiredService<CompetitionSettingsService>();

        Assert.Equal(1, await settings.Get<int>("Ticket.MaxTicketsPerSubmission"));
        Assert.True(await settings.Get<bool>("Ticket.EnableAutoRefund"));
    }

    private async Task<PenaltySubmissionFixture> CreatePenaltySubmissionFixture(string name, bool isPenalty = true)
    {
        var password = "Ticket@Test123";
        var user = await TestDataSeeder.CreateUserAsync(factory.Services, TestDataSeeder.RandomName(), password);
        var team = await TestDataSeeder.CreateTeamAsync(factory.Services, user.Id, $"{name} Team {TestDataSeeder.RandomName()}");
        var game = await TestDataSeeder.CreateGameAsync(factory.Services, $"{name} Game {TestDataSeeder.RandomName()}");
        var challenge = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id,
            $"{name} Challenge {TestDataSeeder.RandomName()}", "flag{ticket}");
        var participation = await TestDataSeeder.JoinGameAsync(factory.Services, game.Id, team.Id, user.Id);

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var submission = new Submission
        {
            Answer = "wrong",
            Status = AnswerResult.WrongAnswer,
            SubmitTimeUtc = DateTimeOffset.UtcNow,
            UserId = user.Id,
            TeamId = team.Id,
            ParticipationId = participation.Id,
            GameId = game.Id,
            ChallengeId = challenge.Id
        };

        await context.Submissions.AddAsync(submission);
        await context.SaveChangesAsync();

        var record = new SubmissionRecord
        {
            SubmissionId = submission.Id,
            IsPenalty = isPenalty,
            ScoreChange = isPenalty ? -2 : 0
        };

        await context.SubmissionRecords.AddAsync(record);
        await context.SaveChangesAsync();

        return new(user, game, submission.Id);
    }

    private static async Task<TicketDto> CreateTicket(HttpClient client, PenaltySubmissionFixture fixture)
    {
        var createResponse = await client.PostAsJsonAsync("/api/ext/tickets", new TicketCreateModel
        {
            SubmissionId = fixture.SubmissionId,
            GameId = fixture.Game.Id,
            Description = "Need review with screenshot"
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<TicketDto>(JsonOptions);
        Assert.NotNull(created);
        return created;
    }

    private static async Task Login(HttpClient client, TestDataSeeder.SeededUser user)
    {
        var response = await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = user.UserName, Password = user.Password });
        response.EnsureSuccessStatusCode();
    }

    private static async Task<HttpResponseMessage> UploadImage(HttpClient client, Guid ticketId, string fileName)
    {
        using var form = CreateImageForm(fileName);
        return await client.PostAsync($"/api/ext/tickets/{ticketId}/attachments", form);
    }

    private static MultipartFormDataContent CreateImageForm(params string[] fileNames)
    {
        var form = new MultipartFormDataContent();
        foreach (var fileName in fileNames)
            AddFile(form, PngBytes, fileName, "image/png");

        return form;
    }

    private static MultipartFormDataContent CreateForm(byte[] bytes, string fileName, string contentType)
    {
        var form = new MultipartFormDataContent();
        AddFile(form, bytes, fileName, contentType);
        return form;
    }

    private static void AddFile(MultipartFormDataContent form, byte[] bytes, string fileName, string contentType)
    {
        var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(content, "files", fileName);
    }

    private static byte[] CreatePngBytes()
    {
        using var image = new Image<Rgba32>(1, 1, new Rgba32(255, 0, 0, 255));
        using var stream = new MemoryStream();
        image.SaveAsPng(stream);
        return stream.ToArray();
    }

    static readonly byte[] PngBytes = CreatePngBytes();

    private record PenaltySubmissionFixture(TestDataSeeder.SeededUser User, TestDataSeeder.SeededGame Game,
        int SubmissionId);
}
