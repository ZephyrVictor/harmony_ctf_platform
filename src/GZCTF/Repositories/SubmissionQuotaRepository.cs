using GZCTF.Models.Request.Game;
using GZCTF.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class SubmissionQuotaRepository(AppDbContext context) : RepositoryBase(context), ISubmissionQuotaRepository
{
    public Task AcquireLock(SubmissionControlScope scope, int gameId, int challengeId, int participationId,
        DateOnly? day, CancellationToken token = default)
    {
        var key = AdvisoryLockKey(scope, gameId, challengeId, participationId, day);
        return Context.Database.ExecuteSqlRawAsync(
            "SELECT pg_advisory_xact_lock({0})",
            [key],
            cancellationToken: token);
    }

    internal static long AdvisoryLockKey(SubmissionControlScope scope, int gameId, int challengeId, int participationId,
        DateOnly? day)
    {
        var scopedChallengeId = scope == SubmissionControlScope.GameDailyTeam ? 0 : challengeId;

        unchecked
        {
            const ulong offset = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;
            var hash = offset;

            Add((int)scope);
            Add(gameId);
            Add(scopedChallengeId);
            Add(participationId);
            Add(day?.DayNumber ?? 0);

            return (long)hash;

            void Add(int value)
            {
                hash ^= (uint)value;
                hash *= prime;
            }
        }
    }
}
