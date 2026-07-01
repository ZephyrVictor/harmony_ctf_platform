using System.Net.Mime;
using System.Security.Claims;
using GZCTF.Middlewares;
using GZCTF.Models.Data;
using GZCTF.Models.Request.Edit;
using GZCTF.Models.Response;
using GZCTF.Services.Cache;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Controllers;

/// <summary>
/// 队伍分数调整 API（管理员）—— 支持手动调分和 0day 加分，所有记录写入 ScoreAdjustment 账本
/// </summary>
[ApiController]
[Route("api/admin/games/{gameId}/score-adjustments")]
[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
public class ScoreAdjustmentController(AppDbContext context, CacheHelper cacheHelper) : ControllerBase
{
    /// <summary>创建调分记录（手动或 0day 加分）</summary>
    [RequireAdmin]
    [HttpPost]
    [ProducesResponseType(typeof(ScoreAdjustmentDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        [FromRoute] int gameId,
        [FromBody] ScoreAdjustmentCreateModel model,
        CancellationToken token)
    {
        if (model.Delta == 0)
            return BadRequest(new RequestResponse("Delta cannot be zero"));

        if (string.IsNullOrWhiteSpace(model.Reason))
            return BadRequest(new RequestResponse("Reason is required"));

        var game = await context.Games.FindAsync([gameId], token);
        if (game is null)
            return NotFound(new RequestResponse("Game not found"));

        var team = await context.Teams.FindAsync([model.TeamId], token);
        if (team is null)
            return NotFound(new RequestResponse("Team not found"));

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized(new RequestResponse("User not found"));

        var adjustment = new ScoreAdjustment
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            TeamId = model.TeamId,
            Delta = model.Delta,
            Reason = model.Reason,
            Category = model.Category,
            RelatedInfo = model.RelatedInfo,
            OperatorId = Guid.Parse(userId),
            CreatedAt = DateTimeOffset.UtcNow
        };

        context.ScoreAdjustments.Add(adjustment);

        // 写入 GameNotice 通报（全员可见）
        var userName = User.Identity?.Name ?? "admin";
        context.GameNotices.Add(new GameNotice
        {
            GameId = gameId,
            Type = NoticeType.ScoreAdjustment,
            Values = new List<string>
            {
                team.Name,
                model.Delta > 0 ? $"+{model.Delta}" : model.Delta.ToString(),
                model.Reason,
                userName
            },
            PublishTimeUtc = DateTimeOffset.UtcNow
        });

        await context.SaveChangesAsync(token);

        // 刷新记分牌缓存，让新分数立即生效
        await cacheHelper.FlushScoreboardCache(gameId, token);

        return CreatedAtAction(nameof(GetById),
            new { gameId, id = adjustment.Id },
            ToDto(adjustment));
    }

    /// <summary>获取单条调分记录</summary>
    [RequireAdmin]
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ScoreAdjustmentDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById([FromRoute] int gameId, [FromRoute] Guid id, CancellationToken token)
    {
        var adj = await context.ScoreAdjustments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.GameId == gameId && a.Id == id, token);
        if (adj is null) return NotFound();
        return Ok(ToDto(adj));
    }

    /// <summary>调分记录列表（分页 + 过滤）</summary>
    [RequireAdmin]
    [HttpGet]
    [ProducesResponseType(typeof(ArrayResponse<ScoreAdjustmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromRoute] int gameId,
        [FromQuery] ScoreAdjustmentCategory? category,
        [FromQuery] int? teamId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken token = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 200) pageSize = 20;

        var query = context.ScoreAdjustments
            .AsNoTracking()
            .Where(a => a.GameId == gameId);

        if (category.HasValue) query = query.Where(a => a.Category == category);
        if (teamId.HasValue) query = query.Where(a => a.TeamId == teamId);

        var total = await query.CountAsync(token);
        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new ScoreAdjustmentDto
            {
                Id = a.Id,
                GameId = a.GameId,
                TeamId = a.TeamId,
                Delta = a.Delta,
                Reason = a.Reason,
                Category = a.Category,
                RelatedInfo = a.RelatedInfo,
                OperatorId = a.OperatorId,
                CreatedAt = a.CreatedAt
            })
            .ToArrayAsync(token);

        return Ok(new ArrayResponse<ScoreAdjustmentDto>(items, total));
    }

    /// <summary>搜索队伍（队名/成员/Token 模糊搜）</summary>
    [RequireAdmin]
    [HttpGet("/api/admin/games/{gameId}/teams/search")]
    [ProducesResponseType(typeof(TeamSearchResultDto[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchTeams(
        [FromRoute] int gameId,
        [FromQuery] string q,
        CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Ok(Array.Empty<TeamSearchResultDto>());

        var keyword = q.Trim();

        // 通过 Participation 关联到 Game
        var teamIds = await context.Participations
            .Where(p => p.GameId == gameId)
            .Select(p => p.TeamId)
            .Distinct()
            .ToListAsync(token);

        var teams = await context.Teams
            .AsNoTracking()
            .Where(t => teamIds.Contains(t.Id))
            .Where(t =>
                EF.Functions.ILike(t.Name, $"%{keyword}%") ||
                EF.Functions.ILike(t.InviteToken, $"%{keyword}%") ||
                t.Members!.Any(m =>
                    EF.Functions.ILike(m.UserName!, $"%{keyword}%")))
            .Select(t => new TeamSearchResultDto
            {
                TeamId = t.Id,
                TeamName = t.Name,
                TeamToken = t.InviteToken,
                Members = t.Members!.Select(m => m.UserName!).ToArray()
            })
            .Take(20)
            .ToListAsync(token);

        return Ok(teams);
    }

    private static ScoreAdjustmentDto ToDto(ScoreAdjustment a) => new()
    {
        Id = a.Id,
        GameId = a.GameId,
        TeamId = a.TeamId,
        Delta = a.Delta,
        Reason = a.Reason,
        Category = a.Category,
        RelatedInfo = a.RelatedInfo,
        OperatorId = a.OperatorId,
        CreatedAt = a.CreatedAt
    };
}
