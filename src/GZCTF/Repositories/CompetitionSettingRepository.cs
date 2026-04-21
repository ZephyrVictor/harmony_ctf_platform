using System.Text.Json;
using GZCTF.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class CompetitionSettingRepository(AppDbContext context) : RepositoryBase(context), ICompetitionSettingRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<T?> Get<T>(string key, CancellationToken token = default)
    {
        var setting = await Context.CompetitionSettings
            .FirstOrDefaultAsync(s => s.Key == key, token);

        if (setting is null)
            return default;

        return JsonSerializer.Deserialize<T>(setting.Value, JsonOptions);
    }

    public async Task<T> Get<T>(string key, T defaultValue, CancellationToken token = default)
    {
        var result = await Get<T>(key, token);
        return result ?? defaultValue;
    }

    public async Task Set(string key, object value, CancellationToken token = default)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);
        var setting = await Context.CompetitionSettings
            .FirstOrDefaultAsync(s => s.Key == key, token);

        if (setting is null)
        {
            setting = new CompetitionSetting
            {
                Key = key,
                Value = json
            };
            await Context.CompetitionSettings.AddAsync(setting, token);
        }
        else
        {
            setting.Value = json;
            setting.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await Context.SaveChangesAsync(token);
    }

    public Task<Dictionary<string, string>> GetAll(CancellationToken token = default)
        => Context.CompetitionSettings
            .ToDictionaryAsync(s => s.Key, s => s.Value, token);
}
