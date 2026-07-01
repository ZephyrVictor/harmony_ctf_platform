using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Net.Mime;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.RateLimiting;
using GZCTF.Models;
using GZCTF.Models.Data;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace GZCTF.Middlewares;

/// <summary>
/// The rate limiter middleware
/// </summary>
[ExcludeFromCodeCoverage]
public static class RateLimiter
{
    public enum LimitPolicy
    {
        /// <summary>
        /// Concurrency operation limit
        /// </summary>
        Concurrency,

        /// <summary>
        /// Register limit
        /// </summary>
        Register,

        /// <summary>
        /// Database query limit
        /// </summary>
        Query,

        /// <summary>
        /// Container operation limit
        /// </summary>
        Container,

        /// <summary>
        /// Flag submit limit
        /// </summary>
        Submit,

        /// <summary>
        /// Pow challenge generation limit
        /// </summary>
        PowChallenge
    }

    public static void ConfigureRateLimiter(RateLimiterOptions options)
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId is not null)
                return RateLimitPartition.GetSlidingWindowLimiter(userId,
                    _ => new()
                    {
                        QueueLimit = 60,
                        PermitLimit = 150,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        SegmentsPerWindow = 6
                    });

            var address = context.Connection.RemoteIpAddress;

            if (address is null || IPAddress.IsLoopback(address))
                return RateLimitPartition.GetNoLimiter(IPAddress.Loopback.ToString());

            return RateLimitPartition.GetSlidingWindowLimiter(address.ToString(),
                _ => new()
                {
                    QueueLimit = 60,
                    PermitLimit = 150,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    SegmentsPerWindow = 6
                });
        });
        options.OnRejected = async (context, cancellationToken) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.HttpContext.Response.ContentType = MediaTypeNames.Application.Json;

            var localizer =
                context.HttpContext.RequestServices.GetRequiredService<IStringLocalizer<Program>>();
            var afterSec = (int)TimeSpan.FromMinutes(1).TotalSeconds;

            if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                afterSec = (int)retryAfter.TotalSeconds;

            context.HttpContext.Response.Headers.RetryAfter = afterSec.ToString(NumberFormatInfo.InvariantInfo);

            // 提交限流告警：如果是 submit 端点被限流，写一条 RateLimitAlert GameNotice
            try
            {
                await TryWriteSubmitRateLimitAlert(context.HttpContext, cancellationToken);
            }
            catch
            {
                // 告警失败不影响限流响应
            }

            await context.HttpContext.Response.WriteAsJsonAsync(
                new RequestResponse(localizer[nameof(Resources.Program.RateLimit_TooManyRequests), afterSec],
                    StatusCodes.Status429TooManyRequests
                ), cancellationToken);
        };
        options.AddConcurrencyLimiter(nameof(LimitPolicy.Concurrency), o =>
        {
            o.PermitLimit = 1;
            o.QueueLimit = 20;
        });
        options.AddSlidingWindowLimiter(nameof(LimitPolicy.Register), o =>
        {
            o.QueueLimit = 10;
            o.PermitLimit = 20;
            o.Window = TimeSpan.FromSeconds(150);
            o.QueueProcessingOrder = QueueProcessingOrder.NewestFirst;
            o.SegmentsPerWindow = 5;
        });
        options.AddTokenBucketLimiter(nameof(LimitPolicy.Query), o =>
        {
            o.TokenLimit = 100;
            o.TokensPerPeriod = 10;
            o.ReplenishmentPeriod = TimeSpan.FromSeconds(10);
        });
        options.AddTokenBucketLimiter(nameof(LimitPolicy.PowChallenge), o =>
        {
            o.TokenLimit = 40;
            o.TokensPerPeriod = 5;
            o.ReplenishmentPeriod = TimeSpan.FromSeconds(30);
        });
        options.AddTokenBucketLimiter(nameof(LimitPolicy.Container), o =>
        {
            o.TokenLimit = 120;
            o.TokensPerPeriod = 30;
            o.ReplenishmentPeriod = TimeSpan.FromSeconds(10);
        });
        options.AddTokenBucketLimiter(nameof(LimitPolicy.Submit), o =>
        {
            o.TokenLimit = 100;
            o.TokensPerPeriod = 50;
            o.ReplenishmentPeriod = TimeSpan.FromSeconds(5);
        });
    }

    /// <summary>
    /// 提交端点被限流时写一条 RateLimitAlert GameNotice（admin-only 可见）
    /// 去重：同一队同一题 5 分钟内只记一条
    /// </summary>
    private static readonly Regex SubmitPathRegex =
        new(@"^/api/game/(?<gameId>\d+)/Challenges/(?<challengeId>\d+)$",
            RegexOptions.Compiled);

    private static async Task TryWriteSubmitRateLimitAlert(HttpContext httpContext, CancellationToken token)
    {
        var path = httpContext.Request.Path.Value ?? string.Empty;
        var match = SubmitPathRegex.Match(path);
        if (!match.Success) return;
        if (httpContext.Request.Method != "POST") return;

        var gameId = int.Parse(match.Groups["gameId"].Value);
        var challengeId = int.Parse(match.Groups["challengeId"].Value);

        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return;

        var db = httpContext.RequestServices.GetRequiredService<AppDbContext>();

        // 找 participation + team
        var participation = await (from p in db.Participations
                                   join t in db.Teams on p.TeamId equals t.Id
                                   where p.GameId == gameId && db.Participations
                                       .Any(pp => pp.GameId == gameId
                                                  && pp.Team.Members.Any(m => m.Id == Guid.Parse(userId)))
                                   select new { p.Id, TeamName = t.Name, TeamId = t.Id })
                                   .FirstOrDefaultAsync(token);
        if (participation is null) return;

        // 去重：5 分钟内同一队同一题只记一条
        var since = DateTimeOffset.UtcNow.AddMinutes(-5);
        var exists = await db.GameNotices.AnyAsync(n =>
            n.GameId == gameId &&
            n.Type == NoticeType.RateLimitAlert &&
            n.PublishTimeUtc > since, token);
        if (exists) return;

        var challenge = await db.GameChallenges
            .Where(c => c.Id == challengeId)
            .Select(c => c.Title)
            .FirstOrDefaultAsync(token);

        db.GameNotices.Add(new GameNotice
        {
            GameId = gameId,
            Type = NoticeType.RateLimitAlert,
            Values = new List<string>
            {
                participation.TeamName,
                challenge ?? $"#{challengeId}",
                DateTimeOffset.UtcNow.ToString("O")
            },
            PublishTimeUtc = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(token);
    }
}
