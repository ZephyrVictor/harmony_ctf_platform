using GZCTF.Repositories.Interface;
using GZCTF.Services.Scoring;
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
            .Include(r => r.Submission!)
            .ThenInclude(s => s.Participation)
            .ThenInclude(p => p.Members)
            .Include(r => r.MatchedFlag)
            .FirstOrDefaultAsync(r => r.SubmissionId == submissionId, token);

    public Task<bool> ExistsBySubmissionId(int submissionId, CancellationToken token = default)
        => Context.SubmissionRecords.AnyAsync(r => r.SubmissionId == submissionId, token);

    public Task<SubmissionRecord[]> GetByUser(Guid userId, int? challengeId = null, int? gameId = null,
        int count = 100, int skip = 0, CancellationToken token = default)
    {
        var query = Context.SubmissionRecords
            .Include(r => r.Submission!)
            .ThenInclude(s => s.GameChallenge)
            .Include(r => r.MatchedFlag)
            .Where(r => r.Submission!.UserId == userId);

        if (challengeId.HasValue)
            query = query.Where(r => r.Submission!.ChallengeId == challengeId.Value);

        if (gameId.HasValue)
            query = query.Where(r => r.Submission!.GameId == gameId.Value);

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
                           && r.ScoreChange > 0
                           && r.Submission != null
                           && (r.ChallengeId == challengeId || r.Submission.ChallengeId == challengeId)
                           && (r.ParticipationId == participationId
                               || r.Submission.ParticipationId == participationId), token);

    public Task<bool> HasCorrectSubmission(int challengeId, int participationId, Guid trackId, int level,
        CancellationToken token = default)
        => Context.SubmissionRecords
            .AnyAsync(r => r.TrackId == trackId
                           && r.Level == level
                           && r.ScoreChange > 0
                           && (r.ChallengeId == challengeId
                               || (r.Submission != null && r.Submission.ChallengeId == challengeId))
                           && (r.ParticipationId == participationId
                               || (r.Submission != null && r.Submission.ParticipationId == participationId)), token);

    public Task<Guid[]> GetSolvedFlagIds(int challengeId, int participationId, CancellationToken token = default)
        => Context.SubmissionRecords
            .AsNoTracking()
            .Where(r => r.MatchedFlagId != null
                        && r.ScoreChange > 0
                        && (r.ChallengeId == challengeId
                            || (r.Submission != null && r.Submission.ChallengeId == challengeId))
                        && (r.ParticipationId == participationId
                            || (r.Submission != null && r.Submission.ParticipationId == participationId)))
            .Select(r => r.MatchedFlagId!.Value)
            .Distinct()
            .ToArrayAsync(token);

    public async Task<List<(Guid TrackId, int Level, DateTimeOffset SolvedAt)>> GetSolvedTrackLevels(int challengeId, int participationId,
        CancellationToken token = default)
    {
        var rows = await Context.SubmissionRecords
            .AsNoTracking()
            .Where(r => r.TrackId != null
                        && r.Level != null
                        && r.ScoreChange > 0
                        && (r.ChallengeId == challengeId
                            || (r.Submission != null && r.Submission.ChallengeId == challengeId))
                        && (r.ParticipationId == participationId
                            || (r.Submission != null && r.Submission.ParticipationId == participationId)))
            .Select(r => new
            {
                TrackId = r.TrackId!.Value,
                Level = r.Level!.Value,
                SolvedAt = r.Submission != null ? r.Submission.SubmitTimeUtc : DateTimeOffset.MinValue
            })
            .ToArrayAsync(token);

        // 取每个 (TrackId, Level) 最早的解决时间
        return rows
            .GroupBy(r => (r.TrackId, r.Level))
            .Select(g => (g.Key.TrackId, g.Key.Level, g.Min(r => r.SolvedAt)))
            .ToList();
    }

    public Task<int> CountSubmissionsByTrackLevel(int challengeId, int participationId, Guid trackId, int level,
        CancellationToken token = default) =>
        Context.SubmissionRecords
            .AsNoTracking()
            .Where(r => r.TrackId == trackId
                        && r.Level == level
                        && (r.ChallengeId == challengeId
                            || (r.Submission != null && r.Submission.ChallengeId == challengeId))
                        && (r.ParticipationId == participationId
                            || (r.Submission != null && r.Submission.ParticipationId == participationId)))
            .CountAsync(token);

    /// <summary>
    /// 获取选手在某题目下最近一次有提交记录的 TrackId（用于 ActiveTrackId）。
    /// 若无任何提交，返回 null。
    /// </summary>
    public async Task<Guid?> GetLatestSubmissionTrackId(int challengeId, int participationId,
        CancellationToken token = default)
    {
        var trackId = await Context.SubmissionRecords
            .AsNoTracking()
            .Where(r => r.TrackId != null
                        && (r.ChallengeId == challengeId
                            || (r.Submission != null && r.Submission.ChallengeId == challengeId))
                        && (r.ParticipationId == participationId
                            || (r.Submission != null && r.Submission.ParticipationId == participationId)))
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => r.TrackId)
            .FirstOrDefaultAsync(token);
        return trackId;
    }

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
                c.Difficulty,
                EnabledLevelCount = c.LevelConfigs.Count(l => l.IsEnabled),
                EnabledTrackCount = c.Tracks.Count(t => t.IsEnabled),
                LevelScoreSum = c.LevelConfigs
                    .Where(l => l.IsEnabled)
                    .Sum(l => l.Score)
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
            var score = ChallengeScoreService.CalculateDisplayScore(
                chal.OriginalScore, chal.MinScoreRate, chal.Difficulty, solvedCount,
                ChallengeScoreService.CalculateConfiguredScore(
                    chal.EnabledLevelCount > 0 ? chal.LevelScoreSum : null,
                    chal.EnabledTrackCount));
            totalScore += score;
        }

        return totalScore;
    }
}
