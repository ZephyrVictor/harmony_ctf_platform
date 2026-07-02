using GZCTF.Models.Data;

namespace GZCTF.Repositories.Interface;

public interface IChallengeCooldownRepository : IRepository
{
    Task<ChallengeCooldownState?> Get(int participationId, int challengeId, CancellationToken token = default);
    Task<ChallengeCooldownState> Upsert(ChallengeCooldownState state, CancellationToken token = default);
}
