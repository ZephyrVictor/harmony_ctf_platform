using GZCTF.Models.Data;
using GZCTF.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class ChallengeCooldownRepository(AppDbContext context) : RepositoryBase(context), IChallengeCooldownRepository
{
    const int CooldownAdvisoryLockKey = 0x43544643; // "CTFC"

    public Task AcquireLock(int participationId, int challengeId, CancellationToken token = default) =>
        Context.Database.ExecuteSqlRawAsync(
            "SELECT pg_advisory_xact_lock({0}, {1})",
            [CooldownAdvisoryLockKey, AdvisoryLockKey(participationId, challengeId)],
            cancellationToken: token);

    public Task<ChallengeCooldownState?> Get(int participationId, int challengeId, CancellationToken token = default) =>
        Context.ChallengeCooldownStates
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ParticipationId == participationId && s.ChallengeId == challengeId, token);

    public async Task<ChallengeCooldownState> Upsert(ChallengeCooldownState state, CancellationToken token = default)
    {
        await AcquireLock(state.ParticipationId, state.ChallengeId, token);

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

    public async Task<ChallengeCooldownState> IncrementWrongCount(
        int participationId,
        int challengeId,
        int wrongAnswerLimit,
        int cooldownMinutes,
        CancellationToken token = default)
    {
        await AcquireLock(participationId, challengeId, token);

        var state = await Context.ChallengeCooldownStates
            .FirstOrDefaultAsync(s => s.ParticipationId == participationId
                && s.ChallengeId == challengeId, token);

        if (state is null)
        {
            state = new ChallengeCooldownState
            {
                ParticipationId = participationId,
                ChallengeId = challengeId,
                WrongCount = 0,
                CooldownUntil = null
            };
            await Context.ChallengeCooldownStates.AddAsync(state, token);
        }

        if (state.CooldownUntil is not null && state.CooldownUntil <= DateTimeOffset.UtcNow)
        {
            state.CooldownUntil = null;
            state.WrongCount = 0;
        }

        state.WrongCount += 1;
        state.LastWrongAt = DateTimeOffset.UtcNow;

        if (state.WrongCount >= wrongAnswerLimit)
        {
            state.CooldownUntil = DateTimeOffset.UtcNow.AddMinutes(cooldownMinutes);
            state.WrongCount = 0;
        }

        await Context.SaveChangesAsync(token);
        return state;
    }

    public async Task Clear(int participationId, int challengeId, CancellationToken token = default)
    {
        await AcquireLock(participationId, challengeId, token);

        var state = await Context.ChallengeCooldownStates
            .FirstOrDefaultAsync(s => s.ParticipationId == participationId
                && s.ChallengeId == challengeId, token);

        if (state is null)
            return;

        state.WrongCount = 0;
        state.CooldownUntil = null;
        await Context.SaveChangesAsync(token);
    }

    static int AdvisoryLockKey(int participationId, int challengeId)
        => unchecked((participationId * 397) ^ challengeId);
}
