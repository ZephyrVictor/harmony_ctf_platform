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
}
