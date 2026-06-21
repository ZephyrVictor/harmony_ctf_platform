using GZCTF.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class ChallengeWrongCountStateRepository(AppDbContext context) : RepositoryBase(context), IChallengeWrongCountStateRepository
{
    public async Task<int> GetWrongCount(int participationId, int challengeId, int level, CancellationToken token = default)
    {
        var state = await Context.ChallengeWrongCountStates
            .FirstOrDefaultAsync(s => s.ParticipationId == participationId
                && s.ChallengeId == challengeId
                && s.Level == level, token);
        return state?.WrongCount ?? 0;
    }

    public async Task<int> IncrementWrongCount(int participationId, int challengeId, int level, CancellationToken token = default)
    {
        var state = await Context.ChallengeWrongCountStates
            .FirstOrDefaultAsync(s => s.ParticipationId == participationId
                && s.ChallengeId == challengeId
                && s.Level == level, token);

        if (state is null)
        {
            state = new ChallengeWrongCountState
            {
                ParticipationId = participationId,
                ChallengeId = challengeId,
                Level = level,
                WrongCount = 1,
                LastResetAt = DateTimeOffset.UtcNow
            };
            await Context.ChallengeWrongCountStates.AddAsync(state, token);
        }
        else
        {
            state.WrongCount++;
        }

        await Context.SaveChangesAsync(token);
        return state.WrongCount;
    }

    public async Task ResetAllLevels(int participationId, int challengeId, CancellationToken token = default)
    {
        var states = await Context.ChallengeWrongCountStates
            .Where(s => s.ParticipationId == participationId && s.ChallengeId == challengeId)
            .ToArrayAsync(token);

        foreach (var s in states)
        {
            s.WrongCount = 0;
            s.LastResetAt = DateTimeOffset.UtcNow;
        }

        await Context.SaveChangesAsync(token);
    }
}
