using GZCTF.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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
        try
        {
            await Context.ChallengeTrackLevelBanStates.AddAsync(ban, token);
            await Context.SaveChangesAsync(token);
            return ban;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation
        })
        {
            Context.Entry(ban).State = EntityState.Detached;
            return await Context.ChallengeTrackLevelBanStates
                .SingleAsync(b => b.ParticipationId == ban.ParticipationId
                    && b.ChallengeId == ban.ChallengeId
                    && b.TrackId == ban.TrackId
                    && b.Level == ban.Level, token);
        }
    }
}
