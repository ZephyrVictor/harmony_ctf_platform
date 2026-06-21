namespace GZCTF.Repositories.Interface;

public interface IChallengeFlagRepository : IRepository
{
    Task<ChallengeFlag[]> GetFlags(int challengeId, CancellationToken token = default);
    Task<ChallengeFlag?> GetFlag(Guid flagId, CancellationToken token = default);
    Task<ChallengeFlag> CreateFlag(ChallengeFlag flag, CancellationToken token = default);
    Task<ChallengeFlag> UpdateFlag(ChallengeFlag flag, CancellationToken token = default);
    Task<bool> DeleteFlag(Guid flagId, CancellationToken token = default);
    Task<bool> HasSubmissions(Guid flagId, CancellationToken token = default);
    Task<bool> ExistsByValue(int challengeId, string flagValue, CancellationToken token = default);
    Task<ChallengeFlag[]> GetFlagsByLevel(int challengeId, int level, CancellationToken token = default);
    Task<ChallengeFlag[]> GetFlagsByTrack(Guid trackId, CancellationToken token = default);
    Task<ChallengeFlag[]> GetFlagsByTrackLevel(Guid trackId, int level, CancellationToken token = default);
    Task<int> CountFlagsByLevel(int challengeId, int level, CancellationToken token = default);
}

