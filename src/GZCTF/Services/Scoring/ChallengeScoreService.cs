using GZCTF.Services.Cache;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Services.Scoring;

public class ChallengeScoreService(AppDbContext context, CacheHelper cacheHelper)
{
    public static int? GetEnabledLevelScoreSum(IEnumerable<ChallengeLevelConfig> levelConfigs)
    {
        var enabledScores = levelConfigs
            .Where(c => c.IsEnabled)
            .Select(c => c.Score)
            .ToArray();

        return enabledScores.Length == 0 ? null : enabledScores.Sum();
    }

    public static int? CalculateConfiguredScore(int? enabledLevelScoreSum, int enabledTrackCount)
    {
        if (enabledLevelScoreSum is null)
            return null;

        return enabledLevelScoreSum.Value * Math.Max(enabledTrackCount, 1);
    }

    public static int CalculateDisplayScore(
        int originalScore,
        double minScoreRate,
        double difficulty,
        int acceptedCount,
        int? configuredScore) =>
        configuredScore
        ?? GameChallenge.CalculateChallengeScore(originalScore, minScoreRate, difficulty, acceptedCount);

    internal static int CalculateImportLevelScoreSum(IEnumerable<GZCTF.Services.Import.LevelColumnMapping> levelColumns) =>
        levelColumns
            .GroupBy(c => c.Level)
            .Select(g => g.Last().Score)
            .Sum();

    public async Task<Dictionary<int, int>> GetConfiguredScores(
        IEnumerable<int> challengeIds,
        CancellationToken token = default)
    {
        var ids = challengeIds.Distinct().ToArray();
        if (ids.Length == 0)
            return [];

        var levelScoreSums = await context.ChallengeLevelConfigs
            .AsNoTracking()
            .Where(c => ids.Contains(c.ChallengeId) && c.IsEnabled)
            .GroupBy(c => c.ChallengeId)
            .Select(g => new { ChallengeId = g.Key, Score = g.Sum(c => c.Score) })
            .ToDictionaryAsync(g => g.ChallengeId, g => g.Score, token);

        var trackCounts = await context.ChallengeTracks
            .AsNoTracking()
            .Where(t => ids.Contains(t.ChallengeId) && t.IsEnabled)
            .GroupBy(t => t.ChallengeId)
            .Select(g => new { ChallengeId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.ChallengeId, g => g.Count, token);

        return levelScoreSums.ToDictionary(
            item => item.Key,
            item => item.Value * Math.Max(trackCounts.GetValueOrDefault(item.Key), 1));
    }

    public async Task<int?> GetConfiguredScore(int challengeId, CancellationToken token = default)
    {
        var enabledScores = await context.ChallengeLevelConfigs
            .AsNoTracking()
            .Where(c => c.ChallengeId == challengeId && c.IsEnabled)
            .Select(c => c.Score)
            .ToArrayAsync(token);

        if (enabledScores.Length == 0)
            return null;

        var enabledTrackCount = await context.ChallengeTracks
            .AsNoTracking()
            .CountAsync(t => t.ChallengeId == challengeId && t.IsEnabled, token);

        return enabledScores.Sum() * Math.Max(enabledTrackCount, 1);
    }

    public async Task<int?> SyncOriginalScoreFromLevels(
        int challengeId,
        CancellationToken token = default,
        bool flushScoreboard = true)
    {
        var challenge = await context.GameChallenges
            .AsNoTracking()
            .Where(c => c.Id == challengeId)
            .Select(c => new { c.GameId, c.OriginalScore })
            .FirstOrDefaultAsync(token);

        if (challenge is null)
            return null;

        var configuredScore = await GetConfiguredScore(challengeId, token);
        if (configuredScore is int score && challenge.OriginalScore != score)
        {
            await context.GameChallenges
                .Where(c => c.Id == challengeId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(c => c.OriginalScore, score), token);
        }

        if (flushScoreboard)
            await cacheHelper.FlushScoreboardCache(challenge.GameId, token);

        return configuredScore;
    }

    public async Task SyncOriginalScoresFromLevels(
        IEnumerable<int> challengeIds,
        CancellationToken token = default,
        bool flushScoreboard = true)
    {
        var ids = challengeIds.Distinct().ToArray();
        if (ids.Length == 0)
            return;

        var challenges = await context.GameChallenges
            .AsNoTracking()
            .Where(c => ids.Contains(c.Id))
            .Select(c => new { c.Id, c.GameId, c.OriginalScore })
            .ToArrayAsync(token);

        var scoreSums = await GetConfiguredScores(ids, token);

        foreach (var challenge in challenges)
        {
            if (!scoreSums.TryGetValue(challenge.Id, out var score) || challenge.OriginalScore == score)
                continue;

            await context.GameChallenges
                .Where(c => c.Id == challenge.Id)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(c => c.OriginalScore, score), token);
        }

        if (!flushScoreboard)
            return;

        foreach (var gameId in challenges.Select(c => c.GameId).Distinct())
            await cacheHelper.FlushScoreboardCache(gameId, token);
    }

    public async Task<Dictionary<int, int>> GetEnabledLevelScoreSums(
        IEnumerable<int> challengeIds,
        CancellationToken token = default)
    {
        var ids = challengeIds.Distinct().ToArray();
        if (ids.Length == 0)
            return [];

        return await context.ChallengeLevelConfigs
            .AsNoTracking()
            .Where(c => ids.Contains(c.ChallengeId) && c.IsEnabled)
            .GroupBy(c => c.ChallengeId)
            .Select(g => new { ChallengeId = g.Key, Score = g.Sum(c => c.Score) })
            .ToDictionaryAsync(g => g.ChallengeId, g => g.Score, token);
    }

    public async Task<ZeroDayGateStatus> GetZeroDayGateStatus(
        int gameId,
        int participationId,
        CancellationToken token = default)
    {
        var requiredScore = await context.Games
            .AsNoTracking()
            .Where(g => g.Id == gameId)
            .Select(g => g.ZeroDayNdayScoreRequirement)
            .FirstOrDefaultAsync(token);

        var ndayScore = await GetNdayScore(gameId, participationId, token);
        return new(requiredScore, ndayScore);
    }

    public async Task<ZeroDayGateStatus?> GetZeroDayGateStatusForTeam(
        int gameId,
        int teamId,
        CancellationToken token = default)
    {
        var requiredScore = await context.Games
            .AsNoTracking()
            .Where(g => g.Id == gameId)
            .Select(g => (int?)g.ZeroDayNdayScoreRequirement)
            .FirstOrDefaultAsync(token);

        if (requiredScore is null)
            return null;

        var participationId = await context.Participations
            .AsNoTracking()
            .Where(p => p.GameId == gameId && p.TeamId == teamId)
            .Select(p => (int?)p.Id)
            .FirstOrDefaultAsync(token);

        if (participationId is null)
            return new(requiredScore.Value, 0);

        var ndayScore = await GetNdayScore(gameId, participationId.Value, token);
        return new(requiredScore.Value, ndayScore);
    }

    private async Task<int> GetNdayScore(int gameId, int participationId, CancellationToken token) =>
        await context.SubmissionRecords
            .AsNoTracking()
            .Where(r => r.ParticipationId == participationId
                && r.GameId == gameId
                && r.MatchedFlagId != null
                && r.ScoreChange > 0)
            .SumAsync(r => (int?)r.ScoreChange, token) ?? 0;
}

public readonly record struct ZeroDayGateStatus(int RequiredScore, int NdayScore)
{
    public bool GateEnabled => RequiredScore > 0;
    public bool CanSubmit => !GateEnabled || NdayScore >= RequiredScore;
}
