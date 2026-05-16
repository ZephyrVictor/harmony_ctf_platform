using System.Text.Json;
using GZCTF.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class ChallengePenaltyRepository(
    AppDbContext context,
    ICompetitionSettingRepository settingRepo) : RepositoryBase(context), IChallengePenaltyRepository
{
    public Task<ChallengePenalty?> GetPenalty(int challengeId, CancellationToken token = default)
        => Context.ChallengePenalties
            .FirstOrDefaultAsync(p => p.ChallengeId == challengeId, token);

    public async Task<ChallengePenalty> GetOrCreatePenalty(int challengeId, CancellationToken token = default)
    {
        var existing = await GetPenalty(challengeId, token);
        if (existing is not null)
            return existing;

        var defaultPenalty = await settingRepo.Get("Scoring.DefaultPenaltyScore", 2, token);

        var penalty = new ChallengePenalty
        {
            ChallengeId = challengeId,
            PenaltyScore = defaultPenalty
        };

        await Context.ChallengePenalties.AddAsync(penalty, token);
        await Context.SaveChangesAsync(token);
        return penalty;
    }

    public async Task<ChallengePenalty> UpdatePenalty(ChallengePenalty penalty, CancellationToken token = default)
    {
        penalty.UpdatedAt = DateTimeOffset.UtcNow;
        Context.ChallengePenalties.Update(penalty);
        await Context.SaveChangesAsync(token);
        return penalty;
    }
}
