using GZCTF.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class ZeroDayTicketRepository(AppDbContext context) : RepositoryBase(context), IZeroDayTicketRepository
{
    public async Task<ZeroDayTicket> CreateTicket(ZeroDayTicket ticket, CancellationToken token = default)
    {
        await Context.ZeroDayTickets.AddAsync(ticket, token);
        await Context.SaveChangesAsync(token);
        return await GetTicket(ticket.Id, token) ?? ticket;
    }

    public Task<ZeroDayTicket?> GetTicket(Guid ticketId, CancellationToken token = default)
        => Context.ZeroDayTickets
            .Include(t => t.Game)
            .Include(t => t.Challenge)
            .Include(t => t.User)
            .Include(t => t.Reviewer)
            .FirstOrDefaultAsync(t => t.Id == ticketId, token);

    public Task<ZeroDayTicket[]> GetUserTickets(Guid userId, int? gameId = null, int count = 100, int skip = 0,
        CancellationToken token = default)
    {
        IQueryable<ZeroDayTicket> query = Context.ZeroDayTickets
            .Include(t => t.Game)
            .Include(t => t.Challenge)
            .Where(t => t.UserId == userId);

        if (gameId.HasValue)
            query = query.Where(t => t.GameId == gameId.Value);

        return query
            .OrderByDescending(t => t.CreatedAt)
            .Skip(skip)
            .Take(count)
            .ToArrayAsync(token);
    }

    public Task<ZeroDayTicket[]> GetAllTickets(ZeroDayTicketStatus? status = null, int? gameId = null,
        int count = 100, int skip = 0, CancellationToken token = default)
    {
        IQueryable<ZeroDayTicket> query = Context.ZeroDayTickets
            .Include(t => t.Game)
            .Include(t => t.Challenge)
            .Include(t => t.User)
            .Include(t => t.Reviewer);

        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);

        if (gameId.HasValue)
            query = query.Where(t => t.GameId == gameId.Value);

        return query
            .OrderByDescending(t => t.CreatedAt)
            .Skip(skip)
            .Take(count)
            .ToArrayAsync(token);
    }

    public async Task<ZeroDayTicket> ApproveTicket(Guid ticketId, int bonusScore, string? comment,
        Guid reviewerId, CancellationToken token = default)
    {
        var ticket = await Context.ZeroDayTickets.FindAsync([ticketId], token)
                     ?? throw new InvalidOperationException("Ticket not found");

        ticket.Status = ZeroDayTicketStatus.Approved;
        ticket.ReviewerId = reviewerId;
        ticket.ReviewComment = comment;
        ticket.BonusScore = bonusScore;
        ticket.ReviewedAt = DateTimeOffset.UtcNow;

        await Context.SaveChangesAsync(token);
        return await GetTicket(ticketId, token) ?? ticket;
    }

    public async Task<ZeroDayTicket> RejectTicket(Guid ticketId, string reason, Guid reviewerId,
        CancellationToken token = default)
    {
        var ticket = await Context.ZeroDayTickets.FindAsync([ticketId], token)
                     ?? throw new InvalidOperationException("Ticket not found");

        ticket.Status = ZeroDayTicketStatus.Rejected;
        ticket.ReviewerId = reviewerId;
        ticket.ReviewComment = reason;
        ticket.ReviewedAt = DateTimeOffset.UtcNow;

        await Context.SaveChangesAsync(token);
        return await GetTicket(ticketId, token) ?? ticket;
    }
}
