using GZCTF.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class ChallengeLockStateRepository(AppDbContext context) : RepositoryBase(context), IChallengeLockStateRepository
{
    public Task<ChallengeLockState?> GetActiveLock(int participationId, int challengeId, CancellationToken token = default) =>
        Context.ChallengeLockStates
            .Where(l => l.ParticipationId == participationId
                && l.ChallengeId == challengeId
                && l.UnlocksAt > DateTimeOffset.UtcNow)
            .OrderByDescending(l => l.UnlocksAt)
            .FirstOrDefaultAsync(token);

    public Task<List<ChallengeLockState>> GetExpiredLocks(int participationId, int challengeId, CancellationToken token = default) =>
        Context.ChallengeLockStates
            .Where(l => l.ParticipationId == participationId
                && l.ChallengeId == challengeId
                && l.UnlocksAt <= DateTimeOffset.UtcNow)
            .ToListAsync(token);

    public async Task<ChallengeLockState> CreateLock(ChallengeLockState lockState, CancellationToken token = default)
    {
        await Context.ChallengeLockStates.AddAsync(lockState, token);
        await Context.SaveChangesAsync(token);
        return lockState;
    }

    public async Task DeleteLocks(IEnumerable<ChallengeLockState> locks, CancellationToken token = default)
    {
        Context.ChallengeLockStates.RemoveRange(locks);
        await Context.SaveChangesAsync(token);
    }
}
