using GZCTF.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class ChallengeFlagRepository(AppDbContext context) : RepositoryBase(context), IChallengeFlagRepository
{
    public Task<ChallengeFlag[]> GetFlags(int challengeId, CancellationToken token = default)
        => Context.ChallengeFlags
            .Where(f => f.ChallengeId == challengeId)
            .OrderBy(f => f.SortOrder)
            .ToArrayAsync(token);

    public Task<ChallengeFlag?> GetFlag(Guid flagId, CancellationToken token = default)
        => Context.ChallengeFlags
            .FirstOrDefaultAsync(f => f.Id == flagId, token);

    public async Task<ChallengeFlag> CreateFlag(ChallengeFlag flag, CancellationToken token = default)
    {
        await Context.ChallengeFlags.AddAsync(flag, token);
        await Context.SaveChangesAsync(token);
        return flag;
    }

    public async Task<ChallengeFlag> UpdateFlag(ChallengeFlag flag, CancellationToken token = default)
    {
        flag.UpdatedAt = DateTimeOffset.UtcNow;
        Context.ChallengeFlags.Update(flag);
        await Context.SaveChangesAsync(token);
        return flag;
    }

    public async Task<bool> DeleteFlag(Guid flagId, CancellationToken token = default)
    {
        var flag = await Context.ChallengeFlags.FindAsync([flagId], token);
        if (flag is null)
            return false;

        Context.ChallengeFlags.Remove(flag);
        await Context.SaveChangesAsync(token);
        return true;
    }

    public Task<bool> HasSubmissions(Guid flagId, CancellationToken token = default)
        => Context.SubmissionRecords
            .AnyAsync(r => r.MatchedFlagId == flagId, token);

    public Task<bool> ExistsByValue(int challengeId, string flagValue, CancellationToken token = default)
        => Context.ChallengeFlags
            .AnyAsync(f => f.ChallengeId == challengeId && f.FlagValue == flagValue, token);
}
