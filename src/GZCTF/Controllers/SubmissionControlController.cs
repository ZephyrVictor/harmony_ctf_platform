using System.Net.Mime;
using GZCTF.Middlewares;
using GZCTF.Models.Response;
using GZCTF.Services.Scoring;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Controllers;

[ApiController]
[Route("api/admin/games/{gameId:int}/submission-control")]
[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
public class SubmissionControlController(AppDbContext context) : ControllerBase
{
    [RequireAdmin]
    [HttpGet]
    [ProducesResponseType(typeof(SubmissionControlOverviewDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Overview([FromRoute] int gameId, CancellationToken token)
    {
        var game = await context.Games.AsNoTracking()
            .Where(g => g.Id == gameId)
            .Select(g => new
            {
                g.Id,
                g.Title,
                g.DailyTeamSubmissionLimit,
                g.CooldownMinutes,
                g.ChallengeCooldownWrongAnswerLimitFactor,
                g.ChallengeDailyTeamSubmissionLimitFactor,
                g.ChallengeTotalTeamSubmissionLimitFactor
            })
            .FirstOrDefaultAsync(token);

        if (game is null)
            return NotFound(new RequestResponse("Game not found"));

        var window = SubmissionQuotaService.GetPrcDayWindow(DateTimeOffset.UtcNow);
        var challenges = await BuildChallengeConfigs(
            gameId,
            game.ChallengeCooldownWrongAnswerLimitFactor,
            game.ChallengeDailyTeamSubmissionLimitFactor,
            game.ChallengeTotalTeamSubmissionLimitFactor,
            token);
        var teams = await BuildTeamSummaries(gameId, game.DailyTeamSubmissionLimit, window.StartUtc, window.EndUtc,
            challenges, token);

        return Ok(new SubmissionControlOverviewDto
        {
            Game = new SubmissionControlGameConfigDto
            {
                GameId = game.Id,
                Title = game.Title,
                DailyTeamSubmissionLimit = game.DailyTeamSubmissionLimit,
                CooldownMinutes = game.CooldownMinutes,
                ChallengeCooldownWrongAnswerLimitFactor = game.ChallengeCooldownWrongAnswerLimitFactor,
                ChallengeDailyTeamSubmissionLimitFactor = game.ChallengeDailyTeamSubmissionLimitFactor,
                ChallengeTotalTeamSubmissionLimitFactor = game.ChallengeTotalTeamSubmissionLimitFactor
            },
            Challenges = challenges,
            Teams = teams,
            DailyResetAt = window.ResetsAtUtc
        });
    }

    [RequireAdmin]
    [HttpPut]
    [ProducesResponseType(typeof(SubmissionControlOverviewDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update([FromRoute] int gameId, [FromBody] SubmissionControlUpdateModel model,
        CancellationToken token)
    {
        var game = await context.Games
            .FirstOrDefaultAsync(g => g.Id == gameId, token);
        if (game is null)
            return NotFound(new RequestResponse("Game not found"));

        var challengeIds = model.Challenges.Select(c => c.ChallengeId).ToHashSet();
        if (challengeIds.Count != model.Challenges.Length)
            return BadRequest(new RequestResponse("Duplicate challenge config"));

        if (model.Challenges.Any(c => !IsValidMode(c.CooldownWrongAnswerLimitMode)
                                      || !IsValidMode(c.DailyTeamSubmissionLimitMode)
                                      || !IsValidMode(c.TotalTeamSubmissionLimitMode)))
            return BadRequest(new RequestResponse("Invalid submission control mode"));

        var challenges = await context.GameChallenges
            .Where(c => c.GameId == gameId
                        && (model.ApplyDefaultsToAllChallenges || challengeIds.Contains(c.Id)))
            .ToDictionaryAsync(c => c.Id, token);

        if (!model.ApplyDefaultsToAllChallenges && challenges.Count != challengeIds.Count)
            return BadRequest(new RequestResponse("Challenge config contains unknown challenge"));
        if (model.ApplyDefaultsToAllChallenges && challengeIds.Any(id => !challenges.ContainsKey(id)))
            return BadRequest(new RequestResponse("Challenge config contains unknown challenge"));

        game.DailyTeamSubmissionLimit = model.DailyTeamSubmissionLimit;
        game.CooldownMinutes = model.CooldownMinutes;
        game.ChallengeCooldownWrongAnswerLimitFactor = model.ChallengeCooldownWrongAnswerLimitFactor;
        game.ChallengeDailyTeamSubmissionLimitFactor = model.ChallengeDailyTeamSubmissionLimitFactor;
        game.ChallengeTotalTeamSubmissionLimitFactor = model.ChallengeTotalTeamSubmissionLimitFactor;

        if (model.ApplyDefaultsToAllChallenges)
        {
            foreach (var challenge in challenges.Values)
            {
                challenge.CooldownWrongAnswerLimitMode = SubmissionControlLimitMode.InheritDefault;
                challenge.DailyTeamSubmissionLimitMode = SubmissionControlLimitMode.InheritDefault;
                challenge.TotalTeamSubmissionLimitMode = SubmissionControlLimitMode.InheritDefault;
            }
        }
        else
        {
            foreach (var item in model.Challenges)
            {
                var challenge = challenges[item.ChallengeId];
                challenge.DailyTeamSubmissionLimit = item.DailyTeamSubmissionLimit;
                challenge.TotalTeamSubmissionLimit = item.TotalTeamSubmissionLimit;
                challenge.CooldownWrongAnswerLimitOverride = item.CooldownWrongAnswerLimitOverride;
                challenge.DailyTeamSubmissionLimitMode = item.DailyTeamSubmissionLimitMode
                                                         ?? LegacyQuotaMode(item.DailyTeamSubmissionLimit);
                challenge.TotalTeamSubmissionLimitMode = item.TotalTeamSubmissionLimitMode
                                                         ?? LegacyQuotaMode(item.TotalTeamSubmissionLimit);
                challenge.CooldownWrongAnswerLimitMode = item.CooldownWrongAnswerLimitMode
                                                         ?? LegacyCooldownMode(item.CooldownWrongAnswerLimitOverride);
            }
        }

        await context.SaveChangesAsync(token);
        return await Overview(gameId, token);
    }

    [RequireAdmin]
    [HttpGet("teams/{teamId:int}")]
    [ProducesResponseType(typeof(SubmissionControlTeamDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> TeamDetail([FromRoute] int gameId, [FromRoute] int teamId, CancellationToken token)
    {
        var game = await context.Games.AsNoTracking()
            .Where(g => g.Id == gameId)
            .Select(g => new
            {
                g.Id,
                g.DailyTeamSubmissionLimit,
                g.CooldownMinutes,
                g.ChallengeCooldownWrongAnswerLimitFactor,
                g.ChallengeDailyTeamSubmissionLimitFactor,
                g.ChallengeTotalTeamSubmissionLimitFactor
            })
            .FirstOrDefaultAsync(token);
        if (game is null)
            return NotFound(new RequestResponse("Game not found"));

        var participation = await context.Participations.AsNoTracking()
            .Where(p => p.GameId == gameId && p.TeamId == teamId)
            .Include(p => p.Team)
            .FirstOrDefaultAsync(token);
        if (participation is null)
            return NotFound(new RequestResponse("Participation not found"));

        var window = SubmissionQuotaService.GetPrcDayWindow(DateTimeOffset.UtcNow);
        var challenges = await BuildChallengeConfigs(
            gameId,
            game.ChallengeCooldownWrongAnswerLimitFactor,
            game.ChallengeDailyTeamSubmissionLimitFactor,
            game.ChallengeTotalTeamSubmissionLimitFactor,
            token);
        var summary = (await BuildTeamSummaries(gameId, game.DailyTeamSubmissionLimit, window.StartUtc, window.EndUtc,
            challenges, token)).FirstOrDefault(t => t.TeamId == teamId);
        if (summary is null)
            return NotFound(new RequestResponse("Team summary not found"));

        var teamChallengeCounts = await context.Submissions.AsNoTracking()
            .Where(s => s.GameId == gameId && s.ParticipationId == participation.Id)
            .GroupBy(s => s.ChallengeId)
            .Select(g => new
            {
                ChallengeId = g.Key,
                Total = g.Count(),
                Today = g.Count(s => s.SubmitTimeUtc >= window.StartUtc && s.SubmitTimeUtc < window.EndUtc)
            })
            .ToArrayAsync(token);

        var countByChallenge = teamChallengeCounts.ToDictionary(c => c.ChallengeId);

        var cooldownRows = await context.ChallengeCooldownStates.AsNoTracking()
            .Where(s => s.ParticipationId == participation.Id)
            .ToArrayAsync(token);
        var cooldownByChallenge = cooldownRows.ToDictionary(c => c.ChallengeId);

        var details = challenges.Select(challenge =>
        {
            countByChallenge.TryGetValue(challenge.ChallengeId, out var counts);
            cooldownByChallenge.TryGetValue(challenge.ChallengeId, out var cooldown);

            var isCooling = cooldown?.CooldownUntil is { } until && until > DateTimeOffset.UtcNow;
            var dailyTeamRemaining = Remaining(challenge.DailyTeamSubmissionLimit, counts?.Today ?? 0);
            var totalTeamRemaining = Remaining(challenge.TotalTeamSubmissionLimit, counts?.Total ?? 0);

            return new SubmissionControlTeamChallengeStatusDto
            {
                ChallengeId = challenge.ChallengeId,
                Title = challenge.Title,
                Category = challenge.Category,
                TodayTeamUsed = counts?.Today ?? 0,
                TotalTeamUsed = counts?.Total ?? 0,
                DailyTeamLimit = challenge.DailyTeamSubmissionLimit,
                DailyTeamRemaining = dailyTeamRemaining,
                DailyTeamExceeded = IsExceeded(challenge.DailyTeamSubmissionLimit, counts?.Today ?? 0),
                TotalTeamLimit = challenge.TotalTeamSubmissionLimit,
                TotalTeamRemaining = totalTeamRemaining,
                TotalTeamExceeded = IsExceeded(challenge.TotalTeamSubmissionLimit, counts?.Total ?? 0),
                WrongCount = cooldown?.WrongCount ?? 0,
                EffectiveWrongAnswerLimit = challenge.EffectiveWrongAnswerLimit,
                CooldownWrongAnswerLimitOverride = challenge.CooldownWrongAnswerLimitOverride,
                CooldownMinutes = game.CooldownMinutes,
                CooldownUntil = cooldown?.CooldownUntil,
                IsCoolingDown = isCooling
            };
        }).ToArray();

        return Ok(new SubmissionControlTeamDetailDto
        {
            ParticipationId = summary.ParticipationId,
            TeamId = summary.TeamId,
            TeamName = summary.TeamName,
            ParticipationStatus = summary.ParticipationStatus,
            TodaySubmissions = summary.TodaySubmissions,
            TotalSubmissions = summary.TotalSubmissions,
            AcceptedSubmissions = summary.AcceptedSubmissions,
            WrongSubmissions = summary.WrongSubmissions,
            GameDailyLimit = summary.GameDailyLimit,
            GameDailyRemaining = summary.GameDailyRemaining,
            GameDailyExceeded = summary.GameDailyExceeded,
            CoolingChallenges = summary.CoolingChallenges,
            BlockedChallenges = summary.BlockedChallenges,
            LastSubmissionAt = summary.LastSubmissionAt,
            Challenges = details
        });
    }

    static async Task<SubmissionControlChallengeConfigDto[]> BuildChallengeConfigs(
        AppDbContext context,
        int gameId,
        int cooldownWrongAnswerFactor,
        int dailyTeamFactor,
        int totalTeamFactor,
        CancellationToken token)
    {
        var challenges = await context.GameChallenges.AsNoTracking()
            .Where(c => c.GameId == gameId)
            .OrderBy(c => c.Category)
            .ThenBy(c => c.Title)
            .Select(c => new
            {
                c.Id,
                c.Title,
                c.Category,
                c.IsEnabled,
                c.CooldownWrongAnswerLimitOverride,
                c.CooldownWrongAnswerLimitMode,
                c.DailyTeamSubmissionLimit,
                c.DailyTeamSubmissionLimitMode,
                c.TotalTeamSubmissionLimit,
                c.TotalTeamSubmissionLimitMode,
                TrackCount = c.Tracks.Count(t => t.IsEnabled)
            })
            .ToArrayAsync(token);

        return challenges.Select(c =>
        {
            var trackCount = c.TrackCount;
            var defaultCooldown = SubmissionControlEffectiveLimits.InheritedLimit(trackCount, cooldownWrongAnswerFactor);
            var defaultDaily = SubmissionControlEffectiveLimits.InheritedLimit(trackCount, dailyTeamFactor);
            var defaultTotal = SubmissionControlEffectiveLimits.InheritedLimit(trackCount, totalTeamFactor);
            var effectiveCooldown = SubmissionControlEffectiveLimits.Resolve(
                c.CooldownWrongAnswerLimitMode,
                c.CooldownWrongAnswerLimitOverride,
                trackCount,
                cooldownWrongAnswerFactor);
            var effectiveDaily = SubmissionControlEffectiveLimits.Resolve(
                c.DailyTeamSubmissionLimitMode,
                c.DailyTeamSubmissionLimit,
                trackCount,
                dailyTeamFactor);
            var effectiveTotal = SubmissionControlEffectiveLimits.Resolve(
                c.TotalTeamSubmissionLimitMode,
                c.TotalTeamSubmissionLimit,
                trackCount,
                totalTeamFactor);

            return new SubmissionControlChallengeConfigDto
            {
                ChallengeId = c.Id,
                Title = c.Title,
                Category = c.Category,
                IsEnabled = c.IsEnabled,
                TrackCount = trackCount,
                EffectiveWrongAnswerLimit = effectiveCooldown,
                CooldownWrongAnswerLimitOverride = c.CooldownWrongAnswerLimitOverride,
                DailyTeamSubmissionLimit = effectiveDaily,
                TotalTeamSubmissionLimit = effectiveTotal,
                CooldownWrongAnswerLimit = new SubmissionControlLimitDto
                {
                    Mode = c.CooldownWrongAnswerLimitMode,
                    CustomValue = c.CooldownWrongAnswerLimitOverride,
                    DefaultValue = defaultCooldown,
                    EffectiveValue = effectiveCooldown
                },
                DailyTeamSubmission = new SubmissionControlLimitDto
                {
                    Mode = c.DailyTeamSubmissionLimitMode,
                    CustomValue = c.DailyTeamSubmissionLimit,
                    DefaultValue = defaultDaily,
                    EffectiveValue = effectiveDaily
                },
                TotalTeamSubmission = new SubmissionControlLimitDto
                {
                    Mode = c.TotalTeamSubmissionLimitMode,
                    CustomValue = c.TotalTeamSubmissionLimit,
                    DefaultValue = defaultTotal,
                    EffectiveValue = effectiveTotal
                }
            };
        }).ToArray();
    }

    Task<SubmissionControlChallengeConfigDto[]> BuildChallengeConfigs(
        int gameId,
        int cooldownWrongAnswerFactor,
        int dailyTeamFactor,
        int totalTeamFactor,
        CancellationToken token) =>
        BuildChallengeConfigs(context, gameId, cooldownWrongAnswerFactor, dailyTeamFactor, totalTeamFactor, token);

    static async Task<SubmissionControlTeamSummaryDto[]> BuildTeamSummaries(
        AppDbContext context,
        int gameId,
        int gameDailyLimit,
        DateTimeOffset dayStart,
        DateTimeOffset dayEnd,
        SubmissionControlChallengeConfigDto[] challenges,
        CancellationToken token)
    {
        var participations = await context.Participations.AsNoTracking()
            .Where(p => p.GameId == gameId)
            .Include(p => p.Team)
            .OrderBy(p => p.Team.Name)
            .ToArrayAsync(token);
        var participationIds = participations.Select(p => p.Id).ToArray();

        var submissionCounts = await context.Submissions.AsNoTracking()
            .Where(s => s.GameId == gameId && participationIds.Contains(s.ParticipationId))
            .GroupBy(s => s.ParticipationId)
            .Select(g => new
            {
                ParticipationId = g.Key,
                Total = g.Count(),
                Today = g.Count(s => s.SubmitTimeUtc >= dayStart && s.SubmitTimeUtc < dayEnd),
                Accepted = g.Count(s => s.Status == AnswerResult.Accepted),
                Wrong = g.Count(s => s.Status != AnswerResult.Accepted),
                Last = g.Max(s => (DateTimeOffset?)s.SubmitTimeUtc)
            })
            .ToArrayAsync(token);
        var countByParticipation = submissionCounts.ToDictionary(c => c.ParticipationId);

        var challengeCounts = await context.Submissions.AsNoTracking()
            .Where(s => s.GameId == gameId && participationIds.Contains(s.ParticipationId))
            .GroupBy(s => new { s.ParticipationId, s.ChallengeId })
            .Select(g => new
            {
                g.Key.ParticipationId,
                g.Key.ChallengeId,
                Total = g.Count(),
                Today = g.Count(s => s.SubmitTimeUtc >= dayStart && s.SubmitTimeUtc < dayEnd)
            })
            .ToArrayAsync(token);
        var challengeCountByParticipation = challengeCounts
            .GroupBy(c => c.ParticipationId)
            .ToDictionary(g => g.Key, g => g.ToDictionary(c => c.ChallengeId));

        var cooldownRows = await context.ChallengeCooldownStates.AsNoTracking()
            .Where(s => participationIds.Contains(s.ParticipationId))
            .ToArrayAsync(token);
        var cooldownLookup = cooldownRows.ToLookup(c => c.ParticipationId);

        return participations.Select(p =>
        {
            countByParticipation.TryGetValue(p.Id, out var counts);
            var today = counts?.Today ?? 0;
            challengeCountByParticipation.TryGetValue(p.Id, out var teamChallengeCounts);

            var gameBlocked = IsExceeded(gameDailyLimit, today);
            var blockedChallenges = gameBlocked
                ? challenges.Length
                : challenges.Count(challenge =>
                {
                    var teamChallengeToday = 0;
                    var teamChallengeTotal = 0;
                    if (teamChallengeCounts is not null
                        && teamChallengeCounts.TryGetValue(challenge.ChallengeId, out var c))
                    {
                        teamChallengeToday = c.Today;
                        teamChallengeTotal = c.Total;
                    }

                    return IsExceeded(challenge.DailyTeamSubmissionLimit, teamChallengeToday)
                    || IsExceeded(challenge.TotalTeamSubmissionLimit, teamChallengeTotal);
                });

            return new SubmissionControlTeamSummaryDto
            {
                ParticipationId = p.Id,
                TeamId = p.TeamId,
                TeamName = p.Team.Name,
                ParticipationStatus = p.Status,
                TodaySubmissions = today,
                TotalSubmissions = counts?.Total ?? 0,
                AcceptedSubmissions = counts?.Accepted ?? 0,
                WrongSubmissions = counts?.Wrong ?? 0,
                GameDailyLimit = gameDailyLimit,
                GameDailyRemaining = Remaining(gameDailyLimit, today),
                GameDailyExceeded = gameBlocked,
                CoolingChallenges = cooldownLookup[p.Id].Count(c =>
                    c.CooldownUntil is { } until && until > DateTimeOffset.UtcNow),
                BlockedChallenges = blockedChallenges,
                LastSubmissionAt = counts?.Last
            };
        }).ToArray();
    }

    Task<SubmissionControlTeamSummaryDto[]> BuildTeamSummaries(int gameId, int gameDailyLimit,
        DateTimeOffset dayStart, DateTimeOffset dayEnd, SubmissionControlChallengeConfigDto[] challenges,
        CancellationToken token) =>
        BuildTeamSummaries(context, gameId, gameDailyLimit, dayStart, dayEnd, challenges, token);

    static int Remaining(int limit, int used) => limit <= 0 ? 0 : Math.Max(0, limit - used);

    static bool IsExceeded(int limit, int used) => limit > 0 && used >= limit;

    static bool IsValidMode(SubmissionControlLimitMode? mode) =>
        mode is null || Enum.IsDefined(typeof(SubmissionControlLimitMode), mode);

    static SubmissionControlLimitMode LegacyQuotaMode(int value) =>
        value > 0 ? SubmissionControlLimitMode.Custom : SubmissionControlLimitMode.Unlimited;

    static SubmissionControlLimitMode LegacyCooldownMode(int value) =>
        value > 0 ? SubmissionControlLimitMode.Custom : SubmissionControlLimitMode.InheritDefault;
}
