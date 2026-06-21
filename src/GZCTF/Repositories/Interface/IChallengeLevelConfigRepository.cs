namespace GZCTF.Repositories.Interface;

public interface IChallengeLevelConfigRepository : IRepository
{
    Task<ChallengeLevelConfig[]> GetConfigs(int challengeId, CancellationToken token = default);
    Task<ChallengeLevelConfig?> GetConfig(int challengeId, int level, CancellationToken token = default);
    Task<ChallengeLevelConfig?> GetConfigById(int configId, CancellationToken token = default);
    Task<ChallengeLevelConfig> CreateConfig(ChallengeLevelConfig config, CancellationToken token = default);
    Task<ChallengeLevelConfig> UpdateConfig(ChallengeLevelConfig config, CancellationToken token = default);
    Task<bool> DeleteConfig(int configId, CancellationToken token = default);
    Task<bool> HasFlags(int challengeId, int level, CancellationToken token = default);
}
