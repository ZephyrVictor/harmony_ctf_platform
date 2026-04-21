namespace GZCTF.Repositories.Interface;

public interface IChallengePenaltyRepository : IRepository
{
    Task<ChallengePenalty?> GetPenalty(int challengeId, CancellationToken token = default);
    Task<ChallengePenalty> GetOrCreatePenalty(int challengeId, CancellationToken token = default);
    Task<ChallengePenalty> UpdatePenalty(ChallengePenalty penalty, CancellationToken token = default);
}
