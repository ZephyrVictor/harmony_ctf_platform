using GZCTF.Models.Data;
using GZCTF.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class ChallengeCooldownRepository(AppDbContext context) : RepositoryBase(context), IChallengeCooldownRepository
{
    public Task<ChallengeCooldownState?> Get(int participationId, int challengeId, CancellationToken token = default) =>
        Context.ChallengeCooldownStates
            .FirstOrDefaultAsync(s => s.ParticipationId == participationId && s.ChallengeId == challengeId, token);

    public async Task<ChallengeCooldownState> Upsert(ChallengeCooldownState state, CancellationToken token = default)
    {
        var existing = await Context.ChallengeCooldownStates
            .FirstOrDefaultAsync(s => s.ParticipationId == state.ParticipationId
                && s.ChallengeId == state.ChallengeId, token);

        if (existing is null)
        {
            await Context.ChallengeCooldownStates.AddAsync(state, token);
            await Context.SaveChangesAsync(token);
            return state;
        }

        existing.WrongCount = state.WrongCount;
        existing.CooldownUntil = state.CooldownUntil;
        existing.LastWrongAt = state.LastWrongAt;
        await Context.SaveChangesAsync(token);
        return existing;
    }
}
