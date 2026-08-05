using GZCTF.Models.Request.Game;
using GZCTF.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Services.Scoring;

public class SubmissionQuotaService(
    AppDbContext context,
    ISubmissionQuotaRepository quotaRepository)
{
    static readonly TimeZoneInfo PrcTimeZone = ResolvePrcTimeZone();

    public async Task<SubmissionQuotaCheck> CheckAndLock(
        Game game,
        GameChallenge challenge,
        Participation participation,
        CancellationToken token = default)
    {
        var window = GetPrcDayWindow(DateTimeOffset.UtcNow);

        foreach (var scope in ConfiguredScopes(game, challenge))
        {
            await quotaRepository.AcquireLock(scope, game.Id, challenge.Id, participation.Id,
                scope is SubmissionControlScope.GameDailyTeam or SubmissionControlScope.ChallengeDailyTeam
                    ? window.Day
                    : null,
                token);
        }

        var status = await GetStatus(game, challenge, participation, null, token);
        var block = FirstBlock(status);
        return new SubmissionQuotaCheck(status, block);
    }

    public async Task<ChallengeSubmissionControlStatus> GetStatus(
        Game game,
        GameChallenge challenge,
        Participation participation,
        CooldownInfo? cooldown,
        CancellationToken token = default)
    {
        var window = GetPrcDayWindow(DateTimeOffset.UtcNow);
        var limits = SubmissionControlEffectiveLimits.ForChallenge(
            game, challenge, await CountEnabledTracks(challenge.Id, token));

        return new ChallengeSubmissionControlStatus
        {
            GameDailyTeam = await BuildStatus(game.DailyTeamSubmissionLimit, window.ResetsAtUtc,
                CountGameDailyTeam(game.Id, participation.Id, window.StartUtc, window.EndUtc), token),
            ChallengeDailyTeam = await BuildStatus(limits.DailyTeamSubmissionLimit, window.ResetsAtUtc,
                CountChallengeDailyTeam(game.Id, challenge.Id, participation.Id, window.StartUtc, window.EndUtc), token),
            ChallengeTotalTeam = await BuildStatus(limits.TotalTeamSubmissionLimit, null,
                CountChallengeTotalTeam(game.Id, challenge.Id, participation.Id), token),
            Cooldown = cooldown
        };
    }

    public static SubmissionQuotaBlock? FirstBlock(ChallengeSubmissionControlStatus status)
    {
        var candidates = new[]
        {
            (SubmissionControlScope.GameDailyTeam, status.GameDailyTeam),
            (SubmissionControlScope.ChallengeTotalTeam, status.ChallengeTotalTeam),
            (SubmissionControlScope.ChallengeDailyTeam, status.ChallengeDailyTeam)
        };

        foreach (var (scope, quota) in candidates)
        {
            if (quota is not { Exceeded: true })
                continue;

            return new SubmissionQuotaBlock(scope, quota.Limit, quota.Used, quota.Remaining, quota.ResetsAt);
        }

        return null;
    }

    internal static PrcDayWindow GetPrcDayWindow(DateTimeOffset utcNow)
    {
        var localNow = TimeZoneInfo.ConvertTime(utcNow, PrcTimeZone);
        var day = DateOnly.FromDateTime(localNow.DateTime);
        var startLocal = day.ToDateTime(TimeOnly.MinValue);
        var endLocal = day.AddDays(1).ToDateTime(TimeOnly.MinValue);
        var startUtc = TimeZoneInfo.ConvertTimeToUtc(startLocal, PrcTimeZone);
        var endUtc = TimeZoneInfo.ConvertTimeToUtc(endLocal, PrcTimeZone);

        return new PrcDayWindow(day, new DateTimeOffset(startUtc, TimeSpan.Zero),
            new DateTimeOffset(endUtc, TimeSpan.Zero));
    }

    static TimeZoneInfo ResolvePrcTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Shanghai");
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.CreateCustomTimeZone("PRC", TimeSpan.FromHours(8), "PRC", "PRC");
            }
            catch (InvalidTimeZoneException)
            {
                return TimeZoneInfo.CreateCustomTimeZone("PRC", TimeSpan.FromHours(8), "PRC", "PRC");
            }
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.CreateCustomTimeZone("PRC", TimeSpan.FromHours(8), "PRC", "PRC");
        }
    }

    IEnumerable<SubmissionControlScope> ConfiguredScopes(Game game, GameChallenge challenge)
    {
        var enabledTrackCount = context.ChallengeTracks
            .Count(t => t.ChallengeId == challenge.Id && t.IsEnabled);
        var limits = SubmissionControlEffectiveLimits.ForChallenge(game, challenge, enabledTrackCount);

        if (game.DailyTeamSubmissionLimit > 0)
            yield return SubmissionControlScope.GameDailyTeam;
        if (limits.TotalTeamSubmissionLimit > 0)
            yield return SubmissionControlScope.ChallengeTotalTeam;
        if (limits.DailyTeamSubmissionLimit > 0)
            yield return SubmissionControlScope.ChallengeDailyTeam;
    }

    static async Task<SubmissionQuotaStatus?> BuildStatus(int limit, DateTimeOffset? resetsAt,
        IQueryable<Submission> query, CancellationToken token)
    {
        if (limit <= 0)
            return null;

        var used = await query.CountAsync(token);
        return new SubmissionQuotaStatus
        {
            Limit = limit,
            Used = used,
            Remaining = Math.Max(0, limit - used),
            Unlimited = false,
            Exceeded = used >= limit,
            ResetsAt = resetsAt
        };
    }

    IQueryable<Submission> CountGameDailyTeam(int gameId, int participationId, DateTimeOffset startUtc,
        DateTimeOffset endUtc) =>
        context.Submissions.AsNoTracking()
            .Where(s => s.GameId == gameId
                        && s.ParticipationId == participationId
                        && s.SubmitTimeUtc >= startUtc
                        && s.SubmitTimeUtc < endUtc);

    IQueryable<Submission> CountChallengeDailyTeam(int gameId, int challengeId, int participationId,
        DateTimeOffset startUtc, DateTimeOffset endUtc) =>
        context.Submissions.AsNoTracking()
            .Where(s => s.GameId == gameId
                        && s.ChallengeId == challengeId
                        && s.ParticipationId == participationId
                        && s.SubmitTimeUtc >= startUtc
                        && s.SubmitTimeUtc < endUtc);

    IQueryable<Submission> CountChallengeTotalTeam(int gameId, int challengeId, int participationId) =>
        context.Submissions.AsNoTracking()
            .Where(s => s.GameId == gameId
                        && s.ChallengeId == challengeId
                        && s.ParticipationId == participationId);

    Task<int> CountEnabledTracks(int challengeId, CancellationToken token) =>
        context.ChallengeTracks.AsNoTracking()
            .CountAsync(t => t.ChallengeId == challengeId && t.IsEnabled, token);
}

public record SubmissionQuotaCheck(ChallengeSubmissionControlStatus Status, SubmissionQuotaBlock? Block);

public record PrcDayWindow(DateOnly Day, DateTimeOffset StartUtc, DateTimeOffset EndUtc)
{
    public DateTimeOffset ResetsAtUtc => EndUtc;
}
