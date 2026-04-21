using GZCTF.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class BigScreenEventRepository(AppDbContext context) : RepositoryBase(context), IBigScreenEventRepository
{
    public async Task<BigScreenEvent> CreateEvent(BigScreenEvent evt, CancellationToken token = default)
    {
        await Context.BigScreenEvents.AddAsync(evt, token);
        await Context.SaveChangesAsync(token);
        return evt;
    }

    public Task<BigScreenEvent[]> GetUnconsumedEvents(int count = 100, CancellationToken token = default)
        => Context.BigScreenEvents
            .Where(e => !e.IsConsumed)
            .OrderByDescending(e => e.CreatedAt)
            .Take(count)
            .ToArrayAsync(token);

    public Task<BigScreenEvent[]> GetAllEvents(int count = 100, CancellationToken token = default)
        => Context.BigScreenEvents
            .OrderByDescending(e => e.CreatedAt)
            .Take(count)
            .ToArrayAsync(token);

    public async Task MarkConsumed(Guid eventId, CancellationToken token = default)
    {
        var evt = await Context.BigScreenEvents.FindAsync([eventId], token);
        if (evt is not null)
        {
            evt.IsConsumed = true;
            await Context.SaveChangesAsync(token);
        }
    }
}
