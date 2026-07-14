using System.Net.Mime;
using System.Security.Claims;
using GZCTF.Middlewares;
using GZCTF.Repositories.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Controllers;

[ApiController]
[Route("api/ext/zeroday-tickets")]
[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
public class ZeroDayTicketController(IZeroDayTicketRepository ticketRepository, AppDbContext dbContext) : ControllerBase
{
    /// <summary>
    /// Create a zero-day vulnerability ticket
    /// </summary>
    [RequireUser]
    [HttpPost]
    [ProducesResponseType(typeof(ZeroDayTicketDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] ZeroDayTicketCreateModel model, CancellationToken token)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized(new RequestResponse("User not found"));

        if (string.IsNullOrWhiteSpace(model.Description))
            return BadRequest(new RequestResponse("Description is required"));

        var userGuid = Guid.Parse(userId);
        var participation = await dbContext.Participations
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.GameId == model.GameId
                && p.Members.Any(m => m.UserId == userGuid), token);
        if (participation is null)
            return BadRequest(new RequestResponse("Team has not joined this game"));

        var gate = await dbContext.Games
            .AsNoTracking()
            .Where(g => g.Id == model.GameId)
            .Select(g => g.ZeroDayNdayScoreRequirement)
            .FirstOrDefaultAsync(token);
        if (gate > 0)
        {
            var ndayScore = await dbContext.SubmissionRecords
                .AsNoTracking()
                .Where(r => r.ParticipationId == participation.Id
                    && r.GameId == model.GameId
                    && r.MatchedFlagId != null
                    && r.ScoreChange > 0)
                .SumAsync(r => (int?)r.ScoreChange, token) ?? 0;
            if (ndayScore < gate)
                return BadRequest(new RequestResponse($"0day submission requires at least {gate} Nday points. Current Nday score: {ndayScore}."));
        }

        var ticket = new ZeroDayTicket
        {
            Id = Guid.NewGuid(),
            Title = model.Title,
            Description = model.Description,
            GameId = model.GameId,
            ChallengeId = model.ChallengeId,
            UserId = userGuid,
            Severity = model.Severity,
            Module = model.Module,
            FilePath = model.FilePath,
            LineNumber = model.LineNumber,
            CommitHash = model.CommitHash,
            CWE = model.CWE,
            PoC = model.PoC
        };

        ticket = await ticketRepository.CreateTicket(ticket, token);
        return CreatedAtAction(nameof(GetTicket), new { ticketId = ticket.Id }, ToDto(ticket));
    }

    /// <summary>
    /// Get current user's zero-day tickets
    /// </summary>
    [RequireUser]
    [HttpGet("mine")]
    [ProducesResponseType(typeof(ZeroDayTicketDto[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyTickets(
        [FromQuery] int? gameId = null,
        [FromQuery] int count = 100,
        [FromQuery] int skip = 0,
        CancellationToken token = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized(new RequestResponse("User not found"));

        var tickets = await ticketRepository.GetUserTickets(Guid.Parse(userId), gameId, count, skip, token);
        return Ok(tickets.Select(ToDto));
    }

    /// <summary>
    /// Get a single zero-day ticket by ID
    /// </summary>
    [RequireUser]
    [HttpGet("{ticketId:guid}")]
    [ProducesResponseType(typeof(ZeroDayTicketDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTicket([FromRoute] Guid ticketId, CancellationToken token)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("Admin");

        var ticket = await ticketRepository.GetTicket(ticketId, token);
        if (ticket is null)
            return NotFound(new RequestResponse("Ticket not found"));

        if (!isAdmin && ticket.UserId != Guid.Parse(userId!))
            return Forbid();

        return Ok(ToDto(ticket));
    }

    /// <summary>
    /// Get all zero-day tickets (admin)
    /// </summary>
    [RequireAdmin]
    [HttpGet]
    [ProducesResponseType(typeof(ZeroDayTicketDto[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllTickets(
        [FromQuery] ZeroDayTicketStatus? status = null,
        [FromQuery] int? gameId = null,
        [FromQuery] int count = 100,
        [FromQuery] int skip = 0,
        CancellationToken token = default)
    {
        var tickets = await ticketRepository.GetAllTickets(status, gameId, count, skip, token);
        return Ok(tickets.Select(ToDto));
    }

    /// <summary>
    /// Approve a zero-day ticket (admin)
    /// </summary>
    [RequireAdmin]
    [HttpPut("{ticketId:guid}/approve")]
    [ProducesResponseType(typeof(ZeroDayTicketDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Approve(
        [FromRoute] Guid ticketId,
        [FromBody] ZeroDayTicketReviewModel model,
        CancellationToken token)
    {
        var reviewerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (reviewerId is null)
            return Unauthorized(new RequestResponse("User not found"));

        if (model.BonusScore < 0)
            return BadRequest(new RequestResponse("BonusScore must be non-negative"));

        var ticket = await ticketRepository.GetTicket(ticketId, token);
        if (ticket is null)
            return NotFound(new RequestResponse("Ticket not found"));

        if (ticket.Status != ZeroDayTicketStatus.Pending)
            return BadRequest(new RequestResponse("Only pending tickets can be approved"));

        ticket = await ticketRepository.ApproveTicket(
            ticketId, model.BonusScore, model.Comment, Guid.Parse(reviewerId),
            model.ChallengeId, token);

        return Ok(ToDto(ticket));
    }

    /// <summary>
    /// Reject a zero-day ticket (admin)
    /// </summary>
    [RequireAdmin]
    [HttpPut("{ticketId:guid}/reject")]
    [ProducesResponseType(typeof(ZeroDayTicketDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Reject(
        [FromRoute] Guid ticketId,
        [FromBody] ZeroDayTicketReviewModel model,
        CancellationToken token)
    {
        var reviewerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (reviewerId is null)
            return Unauthorized(new RequestResponse("User not found"));

        if (string.IsNullOrWhiteSpace(model.Comment))
            return BadRequest(new RequestResponse("Comment is required for rejection"));

        var ticket = await ticketRepository.GetTicket(ticketId, token);
        if (ticket is null)
            return NotFound(new RequestResponse("Ticket not found"));

        if (ticket.Status != ZeroDayTicketStatus.Pending)
            return BadRequest(new RequestResponse("Only pending tickets can be rejected"));

        ticket = await ticketRepository.RejectTicket(
            ticketId, model.Comment, Guid.Parse(reviewerId), token);

        return Ok(ToDto(ticket));
    }

    private static ZeroDayTicketDto ToDto(ZeroDayTicket ticket) => new()
    {
        Id = ticket.Id,
        Title = ticket.Title,
        Description = ticket.Description,
        Status = ticket.Status,
        ReviewComment = ticket.ReviewComment,
        BonusScore = ticket.BonusScore,
        CreatedAt = ticket.CreatedAt,
        ReviewedAt = ticket.ReviewedAt,
        GameId = ticket.GameId,
        ChallengeId = ticket.ChallengeId,
        UserName = ticket.User?.UserName,
        ReviewerName = ticket.Reviewer?.UserName,
        GameTitle = ticket.Game?.Title,
        ChallengeTitle = ticket.Challenge?.Title,
        Severity = ticket.Severity,
        Module = ticket.Module,
        FilePath = ticket.FilePath,
        LineNumber = ticket.LineNumber,
        CommitHash = ticket.CommitHash,
        CWE = ticket.CWE,
        PoC = ticket.PoC,
        CreatedTrackId = ticket.CreatedTrackId
    };
}

public class ZeroDayTicketCreateModel
{
    public string? Title { get; set; }
    public string Description { get; set; } = string.Empty;
    public int GameId { get; set; }
    public int? ChallengeId { get; set; }

    /// <summary>
    /// 严重度（Critical/High/Medium/Low/None）— 决定加分梯度参考
    /// </summary>
    public ZeroDaySeverity Severity { get; set; } = ZeroDaySeverity.None;

    /// <summary>
    /// 模块/子模块路径
    /// </summary>
    public string? Module { get; set; }

    /// <summary>
    /// 文件路径（L1 主键）
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// 行号或范围（"c:1060", "c:550-559", "c:281、c:286"）
    /// </summary>
    public string? LineNumber { get; set; }

    /// <summary>
    /// 修复 commit hash
    /// </summary>
    public string? CommitHash { get; set; }

    /// <summary>
    /// CWE（多个用 "; " 分隔）
    /// </summary>
    public string? CWE { get; set; }

    /// <summary>
    /// PoC / 佐证
    /// </summary>
    public string? PoC { get; set; }
}

public class ZeroDayTicketReviewModel
{
    public int BonusScore { get; set; }
    public string? Comment { get; set; }

    /// <summary>
    /// Admin-selected ChallengeId for the ticket (used when ticket has no ChallengeId).
    /// If provided, the ticket's ChallengeId is updated and bonus is added to the team's score.
    /// </summary>
    public int? ChallengeId { get; set; }
}

public class ZeroDayTicketDto
{
    public Guid Id { get; set; }
    public string? Title { get; set; }
    public string Description { get; set; } = string.Empty;
    public ZeroDayTicketStatus Status { get; set; }
    public string? ReviewComment { get; set; }
    public int BonusScore { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public int GameId { get; set; }
    public int? ChallengeId { get; set; }
    public string? UserName { get; set; }
    public string? ReviewerName { get; set; }
    public string? GameTitle { get; set; }
    public string? ChallengeTitle { get; set; }
    public ZeroDaySeverity Severity { get; set; }
    public string? Module { get; set; }
    public string? FilePath { get; set; }
    public string? LineNumber { get; set; }
    public string? CommitHash { get; set; }
    public string? CWE { get; set; }
    public string? PoC { get; set; }
    public Guid? CreatedTrackId { get; set; }
}
