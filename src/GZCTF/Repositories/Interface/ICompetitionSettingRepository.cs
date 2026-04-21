namespace GZCTF.Repositories.Interface;

public interface ICompetitionSettingRepository : IRepository
{
    Task<T?> Get<T>(string key, CancellationToken token = default);
    Task<T> Get<T>(string key, T defaultValue, CancellationToken token = default);
    Task Set(string key, object value, CancellationToken token = default);
    Task<Dictionary<string, string>> GetAll(CancellationToken token = default);
}
