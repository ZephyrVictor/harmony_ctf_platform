using GZCTF.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class ChallengeTrackRepository(AppDbContext context) : RepositoryBase(context), IChallengeTrackRepository
{
    public Task<ChallengeTrack[]> GetTracks(int challengeId, CancellationToken token = default) =>
        Context.ChallengeTracks
            .Where(t => t.ChallengeId == challengeId)
            .OrderBy(t => t.SortOrder)
            .ToArrayAsync(token);

    public Task<ChallengeTrack?> GetTrack(Guid trackId, CancellationToken token = default) =>
        Context.ChallengeTracks.FirstOrDefaultAsync(t => t.Id == trackId, token);

    public async Task<ChallengeTrack> CreateTrack(ChallengeTrack track, CancellationToken token = default)
    {
        await Context.ChallengeTracks.AddAsync(track, token);
        await Context.SaveChangesAsync(token);
        return track;
    }

    public async Task<ChallengeTrack> UpdateTrack(ChallengeTrack track, CancellationToken token = default)
    {
        track.UpdatedAt = DateTimeOffset.UtcNow;
        Context.ChallengeTracks.Update(track);
        await Context.SaveChangesAsync(token);
        return track;
    }

    public async Task<bool> DeleteTrack(Guid trackId, CancellationToken token = default)
    {
        var track = await Context.ChallengeTracks.FindAsync([trackId], token);
        if (track is null) return false;

        track.IsEnabled = false;
        track.UpdatedAt = DateTimeOffset.UtcNow;

        // 级联软删除 flags
        var flags = await Context.ChallengeFlags
            .Where(f => f.TrackId == trackId)
            .ToArrayAsync(token);
        foreach (var f in flags) f.IsEnabled = false;

        await Context.SaveChangesAsync(token);
        return true;
    }

    public Task<bool> HasFlags(Guid trackId, CancellationToken token = default) =>
        Context.ChallengeFlags.AnyAsync(f => f.TrackId == trackId, token);

    public Task<bool> HasSubmissions(Guid trackId, CancellationToken token = default) =>
        Context.SubmissionRecords.AnyAsync(r => r.TrackId == trackId, token);
}
