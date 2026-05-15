using System.Text.Json;
using GZCTF.Repositories.Interface;

namespace GZCTF.Services.Scoring;

public class CompetitionSettingsService(ICompetitionSettingRepository repository)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly Dictionary<string, string> Defaults = new()
    {
        ["Scoring.MinScore"] = "-100",
        ["Scoring.DefaultPenaltyScore"] = "0",
        ["Scoring.MaxPenaltyPerSubmission"] = "100",
        ["Ticket.EnableAutoRefund"] = "true",
        ["Ticket.DefaultRefundRatio"] = "1.0",
        ["Ticket.MaxTicketsPerSubmission"] = "1",
        ["BigScreen.EnableBonusEvents"] = "true"
    };

    public async Task<T> Get<T>(string key, CancellationToken token = default)
    {
        var dbSettings = await repository.GetAll(token);
        if (dbSettings.TryGetValue(key, out var dbJson))
            return JsonSerializer.Deserialize<T>(dbJson, JsonOptions)!;

        if (Defaults.TryGetValue(key, out var defaultJson))
            return JsonSerializer.Deserialize<T>(defaultJson, JsonOptions)!;

        return default!;
    }

    public Task Set(string key, object value, CancellationToken token = default)
        => repository.Set(key, value, token);

    public async Task<Dictionary<string, string>> GetAll(CancellationToken token = default)
    {
        var dbSettings = await repository.GetAll(token);

        foreach (var (key, defaultValue) in Defaults)
        {
            if (!dbSettings.ContainsKey(key))
                dbSettings[key] = defaultValue;
        }

        return dbSettings;
    }
}
