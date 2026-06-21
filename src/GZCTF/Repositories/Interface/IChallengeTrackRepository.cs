namespace GZCTF.Repositories.Interface;

public interface IChallengeTrackRepository : IRepository
{
    Task<ChallengeTrack[]> GetTracks(int challengeId, CancellationToken token = default);
    Task<ChallengeTrack?> GetTrack(Guid trackId, CancellationToken token = default);
    Task<ChallengeTrack> CreateTrack(ChallengeTrack track, CancellationToken token = default);
    Task<ChallengeTrack> UpdateTrack(ChallengeTrack track, CancellationToken token = default);
    Task<bool> DeleteTrack(Guid trackId, CancellationToken token = default);  // 软删除
    Task<bool> HasFlags(Guid trackId, CancellationToken token = default);
    Task<bool> HasSubmissions(Guid trackId, CancellationToken token = default);
}
