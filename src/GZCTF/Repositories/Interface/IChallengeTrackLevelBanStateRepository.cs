namespace GZCTF.Repositories.Interface;

public interface IChallengeTrackLevelBanStateRepository : IRepository
{
    Task<bool> IsBanned(int participationId, int challengeId, Guid trackId, int level, CancellationToken token = default);
    Task<ChallengeTrackLevelBanState[]> GetBans(int participationId, int challengeId, CancellationToken token = default);
    Task<ChallengeTrackLevelBanState> CreateBan(ChallengeTrackLevelBanState ban, CancellationToken token = default);
}
