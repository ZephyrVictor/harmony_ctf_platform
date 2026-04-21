using GZCTF.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Repositories;

public class SubmissionRecordRepository(AppDbContext context) : RepositoryBase(context), ISubmissionRecordRepository
{
    public async Task<SubmissionRecord> CreateRecord(SubmissionRecord record, CancellationToken token = default)
    {
        await Context.SubmissionRecords.AddAsync(record, token);
        await Context.SaveChangesAsync(token);
        return record;
    }

    public Task<SubmissionRecord?> GetBySubmissionId(int submissionId, CancellationToken token = default)
        => Context.SubmissionRecords
            .Include(r => r.MatchedFlag)
            .FirstOrDefaultAsync(r => r.SubmissionId == submissionId, token);

    public Task<bool> ExistsBySubmissionId(int submissionId, CancellationToken token = default)
        => Context.SubmissionRecords.AnyAsync(r => r.SubmissionId == submissionId, token);

    public Task<SubmissionRecord[]> GetByUser(Guid userId, int? challengeId = null, int count = 100, int skip = 0,
        CancellationToken token = default)
    {
        var query = Context.SubmissionRecords
            .Include(r => r.Submission!)
            .ThenInclude(s => s.GameChallenge)
            .Include(r => r.MatchedFlag)
            .Where(r => r.Submission!.UserId == userId);

        if (challengeId.HasValue)
            query = query.Where(r => r.Submission!.ChallengeId == challengeId.Value);

        return query
            .OrderByDescending(r => r.CreatedAt)
            .Skip(skip)
            .Take(count)
            .ToArrayAsync(token);
    }

    public Task<bool> HasCorrectSubmission(int challengeId, int participationId, Guid flagId,
        CancellationToken token = default)
        => Context.SubmissionRecords
            .AnyAsync(r => r.MatchedFlagId == flagId
                           && r.Submission != null
                           && r.Submission.ChallengeId == challengeId
                           && r.Submission.ParticipationId == participationId, token);

    public Task<int> GetTotalScoreChange(int participationId, CancellationToken token = default)
        => Context.SubmissionRecords
            .Where(r => r.Submission != null && r.Submission.ParticipationId == participationId)
            .SumAsync(r => (int?)r.ScoreChange, token)
            .ContinueWith(t => t.Result ?? 0);

    public async Task<int> GetGZCTFBaseScore(int participationId, int gameId, CancellationToken token = default)
    {
        // Get all FirstSolves for this participation
        var solves = await Context.FirstSolves
            .AsNoTracking()
            .Where(fs => fs.ParticipationId == participationId)
            .Select(fs => fs.ChallengeId)
            .ToArrayAsync(token);

        if (solves.Length == 0)
            return 0;

        // Get challenge metadata for solved challenges
        var challenges = await Context.GameChallenges
            .AsNoTracking()
            .Where(c => c.GameId == gameId && solves.Contains(c.Id))
            .Select(c => new
            {
                c.Id,
                c.OriginalScore,
                c.MinScoreRate,
                c.Difficulty
            })
            .ToArrayAsync(token);

        // Count total solvers for each challenge
        var solveCounts = await Context.FirstSolves
            .AsNoTracking()
            .Where(fs => solves.Contains(fs.ChallengeId))
            .GroupBy(fs => fs.ChallengeId)
            .Select(g => new { ChallengeId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(k => k.ChallengeId, v => v.Count, token);

        // Calculate dynamic score for each challenge
        var totalScore = 0;
        foreach (var chal in challenges)
        {
            var solvedCount = solveCounts.GetValueOrDefault(chal.Id);
            var score = GameChallenge.CalculateChallengeScore(
                chal.OriginalScore, chal.MinScoreRate, chal.Difficulty, solvedCount);
            totalScore += score;
        }

        return totalScore;
    }
}
