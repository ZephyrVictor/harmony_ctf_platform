namespace GZCTF.Repositories.Interface;

public interface IChallengeWrongCountStateRepository : IRepository
{
    Task<int> GetWrongCount(int participationId, int challengeId, int level, CancellationToken token = default);
    Task<int> IncrementWrongCount(int participationId, int challengeId, int level, CancellationToken token = default);
    Task ResetAllLevels(int participationId, int challengeId, CancellationToken token = default);
}
