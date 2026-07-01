using System.Net.Mime;
using System.Security.Claims;
using GZCTF.Middlewares;
using GZCTF.Models.Data;
using GZCTF.Models.Request.Edit;
using GZCTF.Models.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Controllers;

/// <summary>
/// 封禁管理 API（管理员）—— (Track, Level) 粒度，支持手动封禁/解封
/// trap 自动封也在此管理界面可见，可被管理员解封
/// </summary>
[ApiController]
[Route("api/admin/games/{gameId}/bans")]
[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
public class BanController(AppDbContext context) : ControllerBase
{
    /// <summary>封禁列表（可按队/题过滤）</summary>
    [RequireAdmin]
    [HttpGet]
    [ProducesResponseType(typeof(BanStateDto[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromRoute] int gameId,
        [FromQuery] int? teamId,
        [FromQuery] Guid? trackId,
        CancellationToken token)
    {
        var query = from b in context.ChallengeTrackLevelBanStates.AsNoTracking()
                    join p in context.Participations on b.ParticipationId equals p.Id
                    where p.GameId == gameId
                    select new { b, p.TeamId };

        if (teamId.HasValue)
            query = query.Where(x => x.TeamId == teamId.Value);
        if (trackId.HasValue)
            query = query.Where(x => x.b.TrackId == trackId.Value);

        var bans = await query
            .OrderByDescending(x => x.b.BannedAt)
            .Select(x => new BanStateDto
            {
                Id = x.b.Id,
                ParticipationId = x.b.ParticipationId,
                TeamId = x.TeamId,
                ChallengeId = x.b.ChallengeId,
                TrackId = x.b.TrackId,
                Level = x.b.Level,
                BanAnswer = x.b.BanAnswer,
                Reason = x.b.Reason,
                BannedBy = x.b.BannedBy,
                IsManual = x.b.IsManual,
                BannedAt = x.b.BannedAt
            })
            .ToArrayAsync(token);

        return Ok(bans);
    }

    /// <summary>手动封禁（管理员）</summary>
    [RequireAdmin]
    [HttpPost]
    [ProducesResponseType(typeof(BanStateDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        [FromRoute] int gameId,
        [FromBody] BanCreateModel model,
        CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(model.Reason))
            return BadRequest(new RequestResponse("Reason is required"));

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized(new RequestResponse("User not found"));

        // 通过 TeamId + GameId 找 ParticipationId
        var participation = await context.Participations
            .FirstOrDefaultAsync(p => p.GameId == gameId && p.TeamId == model.TeamId, token);
        if (participation is null)
            return NotFound(new RequestResponse("Participation not found for this team in this game"));

        // 检查是否已封禁（幂等）
        var existing = await context.ChallengeTrackLevelBanStates
            .FirstOrDefaultAsync(b =>
                b.ParticipationId == participation.Id &&
                b.ChallengeId == model.ChallengeId &&
                b.TrackId == model.TrackId &&
                b.Level == model.Level, token);
        if (existing is not null)
            return Ok(ToDto(existing, model.TeamId));

        var ban = new ChallengeTrackLevelBanState
        {
            ParticipationId = participation.Id,
            ChallengeId = model.ChallengeId,
            TrackId = model.TrackId,
            Level = model.Level,
            BanAnswer = string.Empty, // 手动封不设 trap answer
            BannedAt = DateTimeOffset.UtcNow,
            Reason = model.Reason,
            BannedBy = Guid.Parse(userId),
            IsManual = true
        };

        context.ChallengeTrackLevelBanStates.Add(ban);
        await context.SaveChangesAsync(token);

        return Ok(ToDto(ban, model.TeamId));
    }

    /// <summary>解封（trap 误触或手动封都可解）</summary>
    [RequireAdmin]
    [HttpDelete("{banId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(
        [FromRoute] int gameId,
        [FromRoute] int banId,
        CancellationToken token)
    {
        var ban = await context.ChallengeTrackLevelBanStates
            .FirstOrDefaultAsync(b => b.Id == banId, token);
        if (ban is null) return NotFound();

        context.ChallengeTrackLevelBanStates.Remove(ban);
        await context.SaveChangesAsync(token);
        return NoContent();
    }

    private static BanStateDto ToDto(ChallengeTrackLevelBanState b, int teamId) => new()
    {
        Id = b.Id,
        ParticipationId = b.ParticipationId,
        TeamId = teamId,
        ChallengeId = b.ChallengeId,
        TrackId = b.TrackId,
        Level = b.Level,
        BanAnswer = b.BanAnswer,
        Reason = b.Reason,
        BannedBy = b.BannedBy,
        IsManual = b.IsManual,
        BannedAt = b.BannedAt
    };
}
