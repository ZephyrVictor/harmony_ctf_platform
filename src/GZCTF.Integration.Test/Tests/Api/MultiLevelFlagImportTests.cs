using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using GZCTF.Integration.Test.Base;
using GZCTF.Models;
using GZCTF.Models.Data;
using GZCTF.Models.Request.Account;
using GZCTF.Services.Import;
using GZCTF.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GZCTF.Integration.Test.Tests.Api;

/// <summary>
/// Integration tests for FlagImportController.
/// Covers Bug 7 (camelCase JSON), Bug 8 (invalid mode), Bug 9 (invalid JSON),
/// Bug 11 (duplicate flag values), and ImportMode behaviors.
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public class MultiLevelFlagImportTests(GZCTFApplicationFactory factory)
{
    private async Task<(HttpClient client, int challengeId)> SetupAsync()
    {
        var password = "Admin@Pass123";
        var admin = await TestDataSeeder.CreateUserAsync(factory.Services,
            TestDataSeeder.RandomName(), password, role: Role.Admin);

        var game = await TestDataSeeder.CreateGameAsync(factory.Services, "FlagImport Test Game");
        var challenge = await TestDataSeeder.CreateStaticChallengeAsync(factory.Services, game.Id,
            "FlagImport Challenge", "flag{seed}");

        var client = factory.CreateClient();
        var loginResp = await client.PostAsJsonAsync("/api/Account/LogIn",
            new LoginModel { UserName = admin.UserName, Password = password });
        loginResp.EnsureSuccessStatusCode();

        return (client, challenge.Id);
    }

    private static MultipartFormDataContent BuildImportForm(string csv, string mapping, string mode)
    {
        var content = new MultipartFormDataContent
        {
            { new StringContent(mapping, Encoding.UTF8), "mapping" },
            { new StringContent(mode, Encoding.UTF8), "mode" }
        };
        var fileBytes = Encoding.UTF8.GetBytes(csv);
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", "import.csv");
        return content;
    }

    private const string CsvWithTwoTracks = """
        Title,FilePath,L1,L2
        Track One,path1,flag{t1l1},flag{t1l2}
        Track Two,path2,flag{t2l1},flag{t2l2}
        """;

    [Fact]
    public async Task PreviewCsv_ReturnsColumnNamesAndSampleRows()
    {
        var (client, challengeId) = await SetupAsync();

        var form = new MultipartFormDataContent();
        var fileBytes = Encoding.UTF8.GetBytes(CsvWithTwoTracks);
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        form.Add(fileContent, "file", "preview.csv");

        var response = await client.PostAsync(
            $"/api/ext/challenges/{challengeId}/flags/import/preview", form);

        response.EnsureSuccessStatusCode();
        var preview = await response.Content.ReadFromJsonAsync<FlagImportFilePreview>();
        Assert.NotNull(preview);
        Assert.Contains("Title", preview!.ColumnNames);
        Assert.Contains("FilePath", preview.ColumnNames);
        Assert.Contains("L1", preview.ColumnNames);
        Assert.Contains("L2", preview.ColumnNames);
        Assert.Equal(2, preview.SampleRows.Count);
    }

    [Fact]
    public async Task Import_WithCamelCaseMapping_ParsesCorrectly()
    {
        // Bug 7: camelCase JSON mapping should deserialize correctly
        var (client, challengeId) = await SetupAsync();

        var mapping = """{"trackTitleColumn":"Title","trackFilePathColumn":"FilePath","matchBy":"FilePath","levelColumns":[{"level":1,"columnName":"L1","answerType":"Exact","score":100},{"level":2,"columnName":"L2","answerType":"Exact","score":200}]}""";
        var form = BuildImportForm(CsvWithTwoTracks, mapping, "Upsert");

        var response = await client.PostAsync(
            $"/api/ext/challenges/{challengeId}/flags/import", form);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<FlagImportResult>();
        Assert.NotNull(result);
        Assert.Equal(2, result!.TotalRows);
        Assert.Equal(2, result.Imported);
        Assert.Equal(0, result.Failed);
    }

    [Fact]
    public async Task Import_WithInvalidMode_Returns400()
    {
        // Bug 8: invalid mode should return 400, not 500
        var (client, challengeId) = await SetupAsync();

        var mapping = """{"matchBy":"FilePath","levelColumns":[{"level":1,"columnName":"L1"}]}""";
        var form = BuildImportForm(CsvWithTwoTracks, mapping, "BadMode");

        var response = await client.PostAsync(
            $"/api/ext/challenges/{challengeId}/flags/import", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Import_WithInvalidJson_Returns400()
    {
        // Bug 9: malformed JSON should return 400, not 500
        var (client, challengeId) = await SetupAsync();

        var form = BuildImportForm(CsvWithTwoTracks, "NOT_JSON", "Upsert");

        var response = await client.PostAsync(
            $"/api/ext/challenges/{challengeId}/flags/import", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Import_WithDuplicateFlagValues_FailsRow()
    {
        // Bug 11: duplicate flag values within the same challenge should fail.
        // Row 2's duplicate flag triggers a rollback, so only one flag survives in DB.
        var (client, challengeId) = await SetupAsync();

        var csv = """
            Title,FilePath,L1
            Track One,path1,flag{dup}
            Track Two,path2,flag{dup}
            """;
        var mapping = """{"trackFilePathColumn":"FilePath","matchBy":"FilePath","levelColumns":[{"level":1,"columnName":"L1","answerType":"Exact","score":100}]}""";
        var form = BuildImportForm(csv, mapping, "Upsert");

        var response = await client.PostAsync(
            $"/api/ext/challenges/{challengeId}/flags/import", form);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<FlagImportResult>();
        Assert.NotNull(result);
        Assert.Equal(2, result!.TotalRows);
        Assert.Equal(1, result.Failed);
        Assert.Contains(result.Errors, e => e.Error.Contains("already exists"));

        // Verify only one flag actually persists in DB (row 2 was rolled back)
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var count = await context.ChallengeFlags.CountAsync(f =>
            f.ChallengeId == challengeId && f.FlagValue == "flag{dup}");
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Import_UpsertMode_UpdatesExistingTrack()
    {
        var (client, challengeId) = await SetupAsync();

        var mapping = """{"trackTitleColumn":"Title","trackFilePathColumn":"FilePath","matchBy":"FilePath","levelColumns":[{"level":1,"columnName":"L1","answerType":"Exact","score":100}]}""";

        // First import
        var form1 = BuildImportForm(CsvWithTwoTracks, mapping, "Upsert");
        var resp1 = await client.PostAsync($"/api/ext/challenges/{challengeId}/flags/import", form1);
        resp1.EnsureSuccessStatusCode();
        var result1 = await resp1.Content.ReadFromJsonAsync<FlagImportResult>();
        Assert.Equal(2, result1!.Imported);

        // Second import (same file) — should update, not fail
        var form2 = BuildImportForm(CsvWithTwoTracks, mapping, "Upsert");
        var resp2 = await client.PostAsync($"/api/ext/challenges/{challengeId}/flags/import", form2);
        resp2.EnsureSuccessStatusCode();
        var result2 = await resp2.Content.ReadFromJsonAsync<FlagImportResult>();
        Assert.Equal(2, result2!.Updated);
        Assert.Equal(0, result2.Imported);
    }

    [Fact]
    public async Task Import_SkipMode_SkipsExistingTrack()
    {
        var (client, challengeId) = await SetupAsync();

        var mapping = """{"trackTitleColumn":"Title","trackFilePathColumn":"FilePath","matchBy":"FilePath","levelColumns":[{"level":1,"columnName":"L1","answerType":"Exact","score":100}]}""";

        // First import
        var form1 = BuildImportForm(CsvWithTwoTracks, mapping, "Upsert");
        var resp1 = await client.PostAsync($"/api/ext/challenges/{challengeId}/flags/import", form1);
        resp1.EnsureSuccessStatusCode();

        // Second import in Skip mode
        var form2 = BuildImportForm(CsvWithTwoTracks, mapping, "Skip");
        var resp2 = await client.PostAsync($"/api/ext/challenges/{challengeId}/flags/import", form2);
        resp2.EnsureSuccessStatusCode();
        var result2 = await resp2.Content.ReadFromJsonAsync<FlagImportResult>();
        Assert.Equal(2, result2!.Skipped);
        Assert.Equal(0, result2.Failed);
    }

    [Fact]
    public async Task Import_FailMode_FailsOnExistingTrack()
    {
        var (client, challengeId) = await SetupAsync();

        var mapping = """{"trackTitleColumn":"Title","trackFilePathColumn":"FilePath","matchBy":"FilePath","levelColumns":[{"level":1,"columnName":"L1","answerType":"Exact","score":100}]}""";

        // First import
        var form1 = BuildImportForm(CsvWithTwoTracks, mapping, "Upsert");
        await client.PostAsync($"/api/ext/challenges/{challengeId}/flags/import", form1);

        // Second import in Fail mode — should fail rows
        var form2 = BuildImportForm(CsvWithTwoTracks, mapping, "Fail");
        var resp2 = await client.PostAsync($"/api/ext/challenges/{challengeId}/flags/import", form2);
        resp2.EnsureSuccessStatusCode();
        var result2 = await resp2.Content.ReadFromJsonAsync<FlagImportResult>();
        Assert.Equal(2, result2!.Failed);
        Assert.Contains(result2.Errors, e => e.Error.Contains("mode=Fail"));
    }
}
