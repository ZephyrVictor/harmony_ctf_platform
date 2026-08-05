using GZCTF.Models.Request.Game;

namespace GZCTF.Repositories.Interface;

public interface ISubmissionQuotaRepository : IRepository
{
    Task AcquireLock(SubmissionControlScope scope, int gameId, int challengeId, int participationId,
        DateOnly? day, CancellationToken token = default);
}
