namespace GZCTF.Repositories.Interface;

public interface IBigScreenEventRepository : IRepository
{
    Task<BigScreenEvent> CreateEvent(BigScreenEvent evt, CancellationToken token = default);
    Task<BigScreenEvent[]> GetUnconsumedEvents(int count = 100, CancellationToken token = default);
    Task<BigScreenEvent[]> GetAllEvents(int count = 100, CancellationToken token = default);
    Task MarkConsumed(Guid eventId, CancellationToken token = default);
}
