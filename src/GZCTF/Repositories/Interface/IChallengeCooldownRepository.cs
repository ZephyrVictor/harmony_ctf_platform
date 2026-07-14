using GZCTF.Models.Data;

namespace GZCTF.Repositories.Interface;

public interface IChallengeCooldownRepository : IRepository
{
    Task<ChallengeCooldownState?> Get(int participationId, int challengeId, CancellationToken token = default);
    Task<ChallengeCooldownState> Upsert(ChallengeCooldownState state, CancellationToken token = default);
    Task<ChallengeCooldownState> IncrementWrongCount(
        int participationId,
        int challengeId,
        int wrongAnswerLimit,
        int cooldownMinutes,
        CancellationToken token = default);
    Task Clear(int participationId, int challengeId, CancellationToken token = default);
}
