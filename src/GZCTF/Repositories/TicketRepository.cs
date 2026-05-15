using GZCTF.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class TicketRepository(AppDbContext context) : RepositoryBase(context), ITicketRepository
{
    public async Task<Ticket> CreateTicket(Ticket ticket, CancellationToken token = default)
    {
        await Context.Tickets.AddAsync(ticket, token);
        await Context.SaveChangesAsync(token);
        return ticket;
    }

    public Task<Ticket?> GetTicket(Guid ticketId, CancellationToken token = default)
        => Context.Tickets
            .Include(t => t.Submission!)
            .ThenInclude(s => s.Team)
            .Include(t => t.Submission!)
            .ThenInclude(s => s.GameChallenge)
            .Include(t => t.Submission!)
            .ThenInclude(s => s.SubmissionRecord)
            .Include(t => t.User)
            .Include(t => t.Reviewer)
            .FirstOrDefaultAsync(t => t.Id == ticketId, token);

    public Task<Ticket[]> GetUserTickets(Guid userId, int? gameId = null, int count = 100, int skip = 0,
        CancellationToken token = default)
    {
        var query = Context.Tickets
            .Include(t => t.Submission!)
            .ThenInclude(s => s.GameChallenge)
            .Include(t => t.Submission!)
            .ThenInclude(s => s.SubmissionRecord)
            .Where(t => t.UserId == userId);

        if (gameId.HasValue)
            query = query.Where(t => t.Submission != null && t.Submission.GameId == gameId.Value);

        return query
            .OrderByDescending(t => t.CreatedAt)
            .Skip(skip)
            .Take(count)
            .ToArrayAsync(token);
    }

    public Task<Ticket[]> GetAllTickets(TicketStatus? status = null, int count = 100, int skip = 0,
        int? gameId = null, CancellationToken token = default)
    {
        IQueryable<Ticket> query = Context.Tickets
            .Include(t => t.Submission!)
            .ThenInclude(s => s.GameChallenge)
            .Include(t => t.Submission!)
            .ThenInclude(s => s.SubmissionRecord)
            .Include(t => t.User!)
            .Include(t => t.Reviewer!);

        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);

        if (gameId.HasValue)
            query = query.Where(t => t.Submission != null && t.Submission.GameId == gameId.Value);

        return query
            .OrderByDescending(t => t.CreatedAt)
            .Skip(skip)
            .Take(count)
            .ToArrayAsync(token);
    }

    public Task<bool> ExistsBySubmission(int submissionId, CancellationToken token = default)
        => Context.Tickets.AnyAsync(t => t.SubmissionId == submissionId, token);

    public Task<int> CountBySubmission(int submissionId, CancellationToken token = default)
        => Context.Tickets.CountAsync(t => t.SubmissionId == submissionId, token);

    public async Task<Ticket> ApproveTicket(Guid ticketId, int refundScore, int bonusScore, string? comment,
        Guid reviewerId, CancellationToken token = default)
    {
        var ticket = await Context.Tickets.FindAsync([ticketId], token)
                     ?? throw new InvalidOperationException("Ticket not found");

        ticket.Status = TicketStatus.Approved;
        ticket.ReviewerId = reviewerId;
        ticket.ReviewComment = comment;
        ticket.RefundScore = refundScore;
        ticket.BonusScore = bonusScore;
        ticket.ReviewedAt = DateTimeOffset.UtcNow;

        await Context.SaveChangesAsync(token);
        return ticket;
    }

    public async Task<Ticket> RejectTicket(Guid ticketId, string reason, Guid reviewerId,
        CancellationToken token = default)
    {
        var ticket = await Context.Tickets.FindAsync([ticketId], token)
                     ?? throw new InvalidOperationException("Ticket not found");

        ticket.Status = TicketStatus.Rejected;
        ticket.ReviewerId = reviewerId;
        ticket.ReviewComment = reason;
        ticket.ReviewedAt = DateTimeOffset.UtcNow;

        await Context.SaveChangesAsync(token);
        return ticket;
    }
}
