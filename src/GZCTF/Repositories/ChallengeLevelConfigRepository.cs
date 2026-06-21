using GZCTF.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class ChallengeLevelConfigRepository(AppDbContext context) : RepositoryBase(context), IChallengeLevelConfigRepository
{
    public Task<ChallengeLevelConfig[]> GetConfigs(int challengeId, CancellationToken token = default) =>
        Context.ChallengeLevelConfigs
            .Where(c => c.ChallengeId == challengeId)
            .OrderBy(c => c.Level)
            .ToArrayAsync(token);

    public Task<ChallengeLevelConfig?> GetConfig(int challengeId, int level, CancellationToken token = default) =>
        Context.ChallengeLevelConfigs
            .FirstOrDefaultAsync(c => c.ChallengeId == challengeId && c.Level == level, token);

    public Task<ChallengeLevelConfig?> GetConfigById(int configId, CancellationToken token = default) =>
        Context.ChallengeLevelConfigs.FirstOrDefaultAsync(c => c.Id == configId, token);

    public async Task<ChallengeLevelConfig> CreateConfig(ChallengeLevelConfig config, CancellationToken token = default)
    {
        await Context.ChallengeLevelConfigs.AddAsync(config, token);
        await Context.SaveChangesAsync(token);
        return config;
    }

    public async Task<ChallengeLevelConfig> UpdateConfig(ChallengeLevelConfig config, CancellationToken token = default)
    {
        Context.ChallengeLevelConfigs.Update(config);
        await Context.SaveChangesAsync(token);
        return config;
    }

    public async Task<bool> DeleteConfig(int configId, CancellationToken token = default)
    {
        var config = await Context.ChallengeLevelConfigs.FindAsync([configId], token);
        if (config is null) return false;
        Context.ChallengeLevelConfigs.Remove(config);
        await Context.SaveChangesAsync(token);
        return true;
    }

    public Task<bool> HasFlags(int challengeId, int level, CancellationToken token = default) =>
        Context.ChallengeFlags.AnyAsync(f => f.ChallengeId == challengeId && f.Level == level && f.IsEnabled, token);
}
