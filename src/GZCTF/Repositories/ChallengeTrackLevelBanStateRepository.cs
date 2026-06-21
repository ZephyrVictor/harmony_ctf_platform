using GZCTF.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class ChallengeTrackLevelBanStateRepository(AppDbContext context) : RepositoryBase(context), IChallengeTrackLevelBanStateRepository
{
    public Task<bool> IsBanned(int participationId, int challengeId, Guid trackId, int level, CancellationToken token = default) =>
        Context.ChallengeTrackLevelBanStates
            .AnyAsync(b => b.ParticipationId == participationId
                && b.ChallengeId == challengeId
                && b.TrackId == trackId
                && b.Level == level, token);

    public Task<ChallengeTrackLevelBanState[]> GetBans(int participationId, int challengeId, CancellationToken token = default) =>
        Context.ChallengeTrackLevelBanStates
            .Where(b => b.ParticipationId == participationId && b.ChallengeId == challengeId)
            .ToArrayAsync(token);

    public async Task<ChallengeTrackLevelBanState> CreateBan(ChallengeTrackLevelBanState ban, CancellationToken token = default)
    {
        await Context.ChallengeTrackLevelBanStates.AddAsync(ban, token);
        await Context.SaveChangesAsync(token);
        return ban;
    }
}
