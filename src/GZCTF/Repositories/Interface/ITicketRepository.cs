namespace GZCTF.Repositories.Interface;

public interface ITicketRepository : IRepository
{
    Task<Ticket> CreateTicket(Ticket ticket, CancellationToken token = default);
    Task<Ticket?> GetTicket(Guid ticketId, CancellationToken token = default);
    Task<Ticket[]> GetUserTickets(Guid userId, int count = 100, int skip = 0, CancellationToken token = default);
    Task<Ticket[]> GetAllTickets(TicketStatus? status = null, int count = 100, int skip = 0,
        CancellationToken token = default);
    Task<bool> ExistsBySubmission(int submissionId, CancellationToken token = default);
    Task<int> CountBySubmission(int submissionId, CancellationToken token = default);
    Task<Ticket> ApproveTicket(Guid ticketId, int refundScore, int bonusScore, string? comment, Guid reviewerId,
        CancellationToken token = default);
    Task<Ticket> RejectTicket(Guid ticketId, string reason, Guid reviewerId, CancellationToken token = default);
}
