using System.Net.Mime;
using GZCTF.Middlewares;
using GZCTF.Models.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Controllers;

[ApiController]
[Route("api/admin/games/{gameId}/team-progress")]
[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
public class TeamProgressController(AppDbContext context) : ControllerBase
{
    [RequireAdmin]
    [HttpGet]
    [ProducesResponseType(typeof(ArrayResponse<TeamProgressSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromRoute] int gameId,
        [FromQuery] string? q,
        [FromQuery] ParticipationStatus? status,
        [FromQuery] int? teamId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken token = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        IQueryable<Participation> query = context.Participations
            .AsNoTracking()
            .Where(p => p.GameId == gameId)
            .Include(p => p.Team)
            .ThenInclude(t => t.Members);

        if (teamId.HasValue)
            query = query.Where(p => p.TeamId == teamId.Value);

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var keyword = q.Trim();
            query = query.Where(p =>
                EF.Functions.ILike(p.Team.Name, $"%{keyword}%") ||
                EF.Functions.ILike(p.Token, $"%{keyword}%") ||
                EF.Functions.ILike(p.Team.InviteToken, $"%{keyword}%") ||
                p.Team.Members.Any(m => EF.Functions.ILike(m.UserName!, $"%{keyword}%")));
        }

        var total = await query.CountAsync(token);
        var participations = await query
            .OrderBy(p => p.Team.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(token);

        var summaries = await BuildSummaries(gameId, participations, token);
        return Ok(new ArrayResponse<TeamProgressSummaryDto>(summaries, total));
    }

    [RequireAdmin]
    [HttpGet("{teamId:int}")]
    [ProducesResponseType(typeof(TeamProgressDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Detail([FromRoute] int gameId, [FromRoute] int teamId, CancellationToken token)
    {
        var participation = await context.Participations
            .AsNoTracking()
            .Where(p => p.GameId == gameId && p.TeamId == teamId)
            .Include(p => p.Team)
            .ThenInclude(t => t.Members)
            .FirstOrDefaultAsync(token);

        if (participation is null)
            return NotFound(new RequestResponse("Participation not found"));

        var summary = (await BuildSummaries(gameId, [participation], token)).Single();
        var detail = new TeamProgressDetailDto
        {
            ParticipationId = summary.ParticipationId,
            TeamId = summary.TeamId,
            TeamName = summary.TeamName,
            TeamToken = summary.TeamToken,
            ParticipationStatus = summary.ParticipationStatus,
            Members = summary.Members,
            TotalSubmissions = summary.TotalSubmissions,
            AcceptedSubmissions = summary.AcceptedSubmissions,
            WrongSubmissions = summary.WrongSubmissions,
            PenaltySubmissions = summary.PenaltySubmissions,
            SolvedLevels = summary.SolvedLevels,
            TotalLevels = summary.TotalLevels,
            CompletedTracks = summary.CompletedTracks,
            TotalTracks = summary.TotalTracks,
            ZeroDayApprovedCount = summary.ZeroDayApprovedCount,
            ZeroDayScore = summary.ZeroDayScore,
            LastSubmissionAt = summary.LastSubmissionAt,
            Challenges = await BuildChallengeProgress(gameId, participation.Id, token),
            Submissions = await BuildSubmissions(gameId, participation.Id, token),
            ZeroDayBonuses = await BuildZeroDayBonuses(gameId, participation.TeamId, token)
        };

        return Ok(detail);
    }

    private async Task<TeamProgressSummaryDto[]> BuildSummaries(int gameId, Participation[] participations,
        CancellationToken token)
    {
        var participationIds = participations.Select(p => p.Id).ToArray();
        var teamIds = participations.Select(p => p.TeamId).ToArray();

        var submissionStats = await context.Submissions
            .AsNoTracking()
            .Where(s => participationIds.Contains(s.ParticipationId))
            .GroupBy(s => s.ParticipationId)
            .Select(g => new
            {
                ParticipationId = g.Key,
                Total = g.Count(),
                Accepted = g.Count(s => s.Status == AnswerResult.Accepted),
                Last = g.Max(s => (DateTimeOffset?)s.SubmitTimeUtc)
            })
            .ToArrayAsync(token);

        var penaltyStats = await context.SubmissionRecords
            .AsNoTracking()
            .Where(r => (r.ParticipationId != null && participationIds.Contains(r.ParticipationId.Value))
                        || (r.Submission != null && participationIds.Contains(r.Submission.ParticipationId)))
            .Select(r => new
            {
                ParticipationId = r.ParticipationId ?? r.Submission!.ParticipationId,
                r.IsPenalty
            })
            .GroupBy(r => r.ParticipationId)
            .Select(g => new
            {
                ParticipationId = g.Key,
                Penalties = g.Count(r => r.IsPenalty)
            })
            .ToArrayAsync(token);

        var solvedRows = await context.SubmissionRecords
            .AsNoTracking()
            .Where(r => (r.GameId == gameId || (r.Submission != null && r.Submission.GameId == gameId))
                        && ((r.ParticipationId != null && participationIds.Contains(r.ParticipationId.Value))
                            || (r.Submission != null && participationIds.Contains(r.Submission.ParticipationId)))
                        && r.TrackId != null
                        && r.Level != null
                        && r.ScoreChange > 0)
            .Select(r => new
            {
                ParticipationId = r.ParticipationId ?? r.Submission!.ParticipationId,
                TrackId = r.TrackId!.Value,
                Level = r.Level!.Value
            })
            .Distinct()
            .ToArrayAsync(token);

        var zeroDayStats = await context.ScoreAdjustments
            .AsNoTracking()
            .Where(a => a.GameId == gameId
                        && teamIds.Contains(a.TeamId)
                        && a.Category == ScoreAdjustmentCategory.ZeroDayBonus)
            .GroupBy(a => a.TeamId)
            .Select(g => new
            {
                TeamId = g.Key,
                Count = g.Count(),
                Score = g.Sum(a => a.Delta)
            })
            .ToArrayAsync(token);

        var totalProgress = await GetTotalProgressShape(gameId, token);
        var levelSetByTrack = totalProgress.LevelsByTrack;

        var submissionMap = submissionStats.ToDictionary(s => s.ParticipationId);
        var penaltyMap = penaltyStats.ToDictionary(s => s.ParticipationId);
        var zeroDayMap = zeroDayStats.ToDictionary(s => s.TeamId);
        var solvedByParticipation = solvedRows
            .GroupBy(r => r.ParticipationId)
            .ToDictionary(g => g.Key, g => g.Select(r => (r.TrackId, r.Level)).ToHashSet());

        return participations.Select(p =>
        {
            submissionMap.TryGetValue(p.Id, out var sub);
            penaltyMap.TryGetValue(p.Id, out var penalty);
            zeroDayMap.TryGetValue(p.TeamId, out var zeroDay);
            solvedByParticipation.TryGetValue(p.Id, out var solvedSet);
            solvedSet ??= [];

            var completedTracks = levelSetByTrack.Count(kvp =>
                kvp.Value.Count > 0 && kvp.Value.All(level => solvedSet.Contains((kvp.Key, level))));

            return new TeamProgressSummaryDto
            {
                ParticipationId = p.Id,
                TeamId = p.TeamId,
                TeamName = p.Team.Name,
                TeamToken = p.Token,
                ParticipationStatus = p.Status,
                Members = p.Team.Members.Select(m => m.UserName ?? string.Empty).Order().ToArray(),
                TotalSubmissions = sub?.Total ?? 0,
                AcceptedSubmissions = sub?.Accepted ?? 0,
                WrongSubmissions = (sub?.Total ?? 0) - (sub?.Accepted ?? 0),
                PenaltySubmissions = penalty?.Penalties ?? 0,
                SolvedLevels = solvedSet.Count,
                TotalLevels = totalProgress.TotalLevels,
                CompletedTracks = completedTracks,
                TotalTracks = totalProgress.TotalTracks,
                ZeroDayApprovedCount = zeroDay?.Count ?? 0,
                ZeroDayScore = zeroDay?.Score ?? 0,
                LastSubmissionAt = sub?.Last
            };
        }).ToArray();
    }

    private async Task<ProgressShape> GetTotalProgressShape(int gameId, CancellationToken token)
    {
        var rows = await (
            from c in context.GameChallenges.AsNoTracking()
            join t in context.ChallengeTracks.AsNoTracking() on c.Id equals t.ChallengeId
            join l in context.ChallengeLevelConfigs.AsNoTracking() on c.Id equals l.ChallengeId
            where c.GameId == gameId && c.IsEnabled && t.IsEnabled && l.IsEnabled
            select new { t.Id, l.Level }
        ).ToArrayAsync(token);

        var levelsByTrack = rows
            .GroupBy(r => r.Id)
            .ToDictionary(g => g.Key, g => g.Select(r => r.Level).Distinct().ToHashSet());

        return new ProgressShape(levelsByTrack.Count, rows.Distinct().Count(), levelsByTrack);
    }

    private async Task<ChallengeProgressDto[]> BuildChallengeProgress(int gameId, int participationId,
        CancellationToken token)
    {
        var challenges = await context.GameChallenges
            .AsNoTracking()
            .Where(c => c.GameId == gameId)
            .OrderBy(c => c.Category)
            .ThenBy(c => c.Title)
            .Select(c => new
            {
                c.Id,
                c.Title,
                c.Category,
                Tracks = c.Tracks.OrderBy(t => t.SortOrder).ThenBy(t => t.Title).Select(t => new
                {
                    t.Id,
                    t.Title,
                    t.Repository,
                    t.Module,
                    t.FilePath,
                    t.Category,
                    t.IsEnabled
                }).ToArray(),
                Levels = c.LevelConfigs.OrderBy(l => l.SortOrder).ThenBy(l => l.Level).Select(l => new
                {
                    l.Level,
                    l.Title,
                    l.Score,
                    l.IsEnabled
                }).ToArray()
            })
            .ToArrayAsync(token);

        var challengeIds = challenges.Select(c => c.Id).ToArray();
        var records = await context.SubmissionRecords
            .AsNoTracking()
            .Where(r => (r.ParticipationId == participationId
                         || (r.Submission != null && r.Submission.ParticipationId == participationId))
                        && ((r.ChallengeId != null && challengeIds.Contains(r.ChallengeId.Value))
                            || (r.Submission != null && challengeIds.Contains(r.Submission.ChallengeId))))
            .Select(r => new
            {
                r.SubmissionId,
                ChallengeId = r.ChallengeId ?? r.Submission!.ChallengeId,
                r.TrackId,
                r.Level,
                r.ScoreChange,
                r.IsPenalty,
                r.CreatedAt,
                Answer = r.Submission != null ? r.Submission.Answer : string.Empty,
                SubmittedAt = r.Submission != null ? r.Submission.SubmitTimeUtc : r.CreatedAt
            })
            .ToArrayAsync(token);

        return challenges.Select(challenge =>
        {
            var enabledLevels = challenge.Levels.Where(l => l.IsEnabled).ToArray();
            var tracks = challenge.Tracks.Select(track =>
            {
                var levels = enabledLevels.Select(level =>
                {
                    var levelRecords = records
                        .Where(r => r.ChallengeId == challenge.Id && r.TrackId == track.Id && r.Level == level.Level)
                        .ToArray();
                    var accepted = levelRecords
                        .Where(r => r.ScoreChange > 0)
                        .OrderBy(r => r.SubmittedAt)
                        .FirstOrDefault();

                    return new LevelProgressDto
                    {
                        Level = level.Level,
                        Title = level.Title,
                        Score = level.Score,
                        Status = accepted is null ? "Unsolved" : "Solved",
                        SolvedAt = accepted?.SubmittedAt,
                        AcceptedSubmissionId = accepted?.SubmissionId,
                        AcceptedAnswer = accepted?.Answer,
                        Attempts = levelRecords.Length,
                        WrongAttempts = levelRecords.Count(r => r.ScoreChange <= 0 || r.IsPenalty),
                        LatestSubmissionAt = levelRecords
                            .OrderByDescending(r => r.SubmittedAt)
                            .Select(r => (DateTimeOffset?)r.SubmittedAt)
                            .FirstOrDefault()
                    };
                }).ToArray();

                return new TrackProgressDto
                {
                    TrackId = track.Id,
                    Title = track.Title,
                    Repository = track.Repository,
                    Module = track.Module,
                    FilePath = track.FilePath,
                    Category = track.Category,
                    IsEnabled = track.IsEnabled,
                    Levels = levels
                };
            }).ToArray();

            return new ChallengeProgressDto
            {
                ChallengeId = challenge.Id,
                ChallengeTitle = challenge.Title,
                Category = challenge.Category,
                Tracks = tracks
            };
        }).ToArray();
    }

    private Task<TeamSubmissionDto[]> BuildSubmissions(int gameId, int participationId, CancellationToken token) =>
        context.Submissions
            .AsNoTracking()
            .Where(s => s.GameId == gameId && s.ParticipationId == participationId)
            .OrderByDescending(s => s.SubmitTimeUtc)
            .Take(500)
            .Select(s => new TeamSubmissionDto
            {
                SubmissionId = s.Id,
                SubmittedAt = s.SubmitTimeUtc,
                UserName = s.User != null ? s.User.UserName ?? string.Empty : string.Empty,
                ChallengeId = s.ChallengeId,
                ChallengeTitle = s.GameChallenge != null ? s.GameChallenge.Title : string.Empty,
                TrackId = s.SubmissionRecord != null ? s.SubmissionRecord.TrackId : null,
                Level = s.SubmissionRecord != null ? s.SubmissionRecord.Level : null,
                Answer = s.Answer,
                Status = s.Status,
                SubmissionType = s.SubmissionRecord != null ? s.SubmissionRecord.SubmissionType : null,
                ScoreChange = s.SubmissionRecord != null ? s.SubmissionRecord.ScoreChange : 0,
                IsPenalty = s.SubmissionRecord != null && s.SubmissionRecord.IsPenalty
            })
            .ToArrayAsync(token);

    private Task<ZeroDayBonusBriefDto[]> BuildZeroDayBonuses(int gameId, int teamId, CancellationToken token) =>
        context.ScoreAdjustments
            .AsNoTracking()
            .Where(a => a.GameId == gameId && a.TeamId == teamId && a.Category == ScoreAdjustmentCategory.ZeroDayBonus)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new ZeroDayBonusBriefDto
            {
                Id = a.Id,
                Severity = a.ZeroDaySeverity,
                Delta = a.Delta,
                Reason = a.Reason,
                RelatedInfo = a.RelatedInfo,
                CreatedAt = a.CreatedAt
            })
            .ToArrayAsync(token);

    private sealed record ProgressShape(
        int TotalTracks,
        int TotalLevels,
        Dictionary<Guid, HashSet<int>> LevelsByTrack);
}
