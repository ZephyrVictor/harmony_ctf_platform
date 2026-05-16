namespace GZCTF.Repositories.Interface;

public interface IZeroDayTicketRepository : IRepository
{
    Task<ZeroDayTicket> CreateTicket(ZeroDayTicket ticket, CancellationToken token = default);
    Task<ZeroDayTicket?> GetTicket(Guid ticketId, CancellationToken token = default);
    Task<ZeroDayTicket[]> GetUserTickets(Guid userId, int? gameId = null, int count = 100, int skip = 0,
        CancellationToken token = default);
    Task<ZeroDayTicket[]> GetAllTickets(ZeroDayTicketStatus? status = null, int? gameId = null, int count = 100,
        int skip = 0, CancellationToken token = default);
    Task<ZeroDayTicket> ApproveTicket(Guid ticketId, int bonusScore, string? comment, Guid reviewerId,
        CancellationToken token = default);
    Task<ZeroDayTicket> RejectTicket(Guid ticketId, string reason, Guid reviewerId,
        CancellationToken token = default);
}
