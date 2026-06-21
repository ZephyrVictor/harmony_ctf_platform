namespace GZCTF.Repositories.Interface;

public interface IChallengeLockStateRepository : IRepository
{
    Task<ChallengeLockState?> GetActiveLock(int participationId, int challengeId, CancellationToken token = default);
    Task<List<ChallengeLockState>> GetExpiredLocks(int participationId, int challengeId, CancellationToken token = default);
    Task<ChallengeLockState> CreateLock(ChallengeLockState lockState, CancellationToken token = default);
    Task DeleteLocks(IEnumerable<ChallengeLockState> locks, CancellationToken token = default);
}
