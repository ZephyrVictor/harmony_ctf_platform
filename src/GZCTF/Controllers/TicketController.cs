using System.Net.Mime;
using System.Security.Claims;
using GZCTF.Middlewares;
using GZCTF.Models.Request;
using GZCTF.Repositories.Interface;
using GZCTF.Services.Scoring;
using Microsoft.AspNetCore.Mvc;

namespace GZCTF.Controllers;

[ApiController]
[Route("api/ext/tickets")]
[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
public class TicketController(
    ITicketRepository ticketRepository,
    ISubmissionRecordRepository recordRepository,
    ScoringService scoringService,
    CompetitionSettingsService settingsService) : ControllerBase
{
    /// <summary>
    /// Create a ticket for a penalty submission
    /// </summary>
    [RequireUser]
    [HttpPost]
    [ProducesResponseType(typeof(TicketDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] TicketCreateModel model, CancellationToken token)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized(new RequestResponse("User not found"));

        if (string.IsNullOrWhiteSpace(model.Description))
            return BadRequest(new RequestResponse("Description is required"));

        var maxTickets = await settingsService.Get<int>("Ticket.MaxTicketsPerSubmission", token);
        var existingCount = await ticketRepository.CountBySubmission(model.SubmissionId, token);
        if (existingCount >= maxTickets)
            return Conflict(new RequestResponse($"Maximum of {maxTickets} ticket(s) per submission exceeded"));

        var record = await recordRepository.GetBySubmissionId(model.SubmissionId, token);
        if (record is null)
            return BadRequest(new RequestResponse("Submission not found"));

        if (!record.IsPenalty)
            return BadRequest(new RequestResponse("Only penalty submissions can be appealed"));

        var ticket = new Ticket
        {
            Description = model.Description,
            SubmissionId = model.SubmissionId,
            UserId = Guid.Parse(userId)
        };

        ticket = await ticketRepository.CreateTicket(ticket, token);
        return CreatedAtAction(nameof(GetMyTickets), null, ToDto(ticket));
    }

    /// <summary>
    /// Get current user's tickets
    /// </summary>
    [RequireUser]
    [HttpGet("mine")]
    [ProducesResponseType(typeof(TicketDto[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyTickets(
        [FromQuery] int count = 100,
        [FromQuery] int skip = 0,
        CancellationToken token = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized(new RequestResponse("User not found"));

        var tickets = await ticketRepository.GetUserTickets(Guid.Parse(userId), count, skip, token);
        return Ok(tickets.Select(ToDto));
    }

    /// <summary>
    /// Get a single ticket by ID
    /// </summary>
    [RequireUser]
    [HttpGet("{ticketId:guid}")]
    [ProducesResponseType(typeof(TicketDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTicket(
        [FromRoute] Guid ticketId,
        CancellationToken token)
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
    /// Get all tickets (admin)
    /// </summary>
    [RequireAdmin]
    [HttpGet]
    [ProducesResponseType(typeof(TicketDto[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllTickets(
        [FromQuery] TicketStatus? status = null,
        [FromQuery] int count = 100,
        [FromQuery] int skip = 0,
        CancellationToken token = default)
    {
        var tickets = await ticketRepository.GetAllTickets(status, count, skip, token);
        return Ok(tickets.Select(ToDto));
    }

    /// <summary>
    /// Approve a ticket (admin)
    /// </summary>
    [RequireAdmin]
    [HttpPut("{ticketId:guid}/approve")]
    [ProducesResponseType(typeof(TicketDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Approve(
        [FromRoute] Guid ticketId,
        [FromBody] TicketReviewModel model,
        CancellationToken token)
    {
        var reviewerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (reviewerId is null)
            return Unauthorized(new RequestResponse("User not found"));

        if (model.RefundScore < 0)
            return BadRequest(new RequestResponse("RefundScore must be non-negative"));

        if (model.BonusScore < 0)
            return BadRequest(new RequestResponse("BonusScore must be non-negative"));

        var ticket = await ticketRepository.GetTicket(ticketId, token);
        if (ticket is null)
            return NotFound(new RequestResponse("Ticket not found"));

        if (ticket.Status != TicketStatus.Pending)
            return BadRequest(new RequestResponse("Only pending tickets can be approved"));

        var refundScore = model.RefundScore;
        var bonusScore = model.BonusScore;

        var enableAutoRefund = await settingsService.Get<bool>("Ticket.EnableAutoRefund", token);
        if (enableAutoRefund && refundScore == 0)
        {
            var record = await recordRepository.GetBySubmissionId(ticket.SubmissionId, token);
            if (record is not null)
            {
                var ratio = await settingsService.Get<double>("Ticket.DefaultRefundRatio", token);
                refundScore = (int)Math.Round(Math.Abs(record.ScoreChange) * ratio);
            }
        }

        ticket = await ticketRepository.ApproveTicket(
            ticketId, refundScore, bonusScore, model.Comment, Guid.Parse(reviewerId), token);

        await scoringService.ApproveTicket(ticket, refundScore, bonusScore, token);

        return Ok(ToDto(ticket));
    }

    /// <summary>
    /// Reject a ticket (admin)
    /// </summary>
    [RequireAdmin]
    [HttpPut("{ticketId:guid}/reject")]
    [ProducesResponseType(typeof(TicketDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Reject(
        [FromRoute] Guid ticketId,
        [FromBody] TicketReviewModel model,
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

        if (ticket.Status != TicketStatus.Pending)
            return BadRequest(new RequestResponse("Only pending tickets can be rejected"));

        ticket = await ticketRepository.RejectTicket(
            ticketId, model.Comment, Guid.Parse(reviewerId), token);

        return Ok(ToDto(ticket));
    }

    private static TicketDto ToDto(Ticket ticket)
    {
        return new TicketDto
        {
            Id = ticket.Id,
            Description = ticket.Description,
            Status = ticket.Status,
            ReviewComment = ticket.ReviewComment,
            RefundScore = ticket.RefundScore,
            BonusScore = ticket.BonusScore,
            CreatedAt = ticket.CreatedAt,
            ReviewedAt = ticket.ReviewedAt,
            SubmissionId = ticket.SubmissionId,
            UserName = ticket.User?.UserName,
            ReviewerName = ticket.Reviewer?.UserName,
            ChallengeName = ticket.Submission?.GameChallenge?.Title,
            Answer = ticket.Submission?.Answer,
            PenaltyScore = ticket.Submission?.SubmissionRecord?.IsPenalty == true
                ? Math.Abs(ticket.Submission.SubmissionRecord.ScoreChange)
                : 0
        };
    }
}

public class TicketCreateModel
{
    public int SubmissionId { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class TicketReviewModel
{
    public int RefundScore { get; set; }
    public int BonusScore { get; set; }
    public string? Comment { get; set; }
}

public class TicketDto
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public TicketStatus Status { get; set; }
    public string? ReviewComment { get; set; }
    public int RefundScore { get; set; }
    public int BonusScore { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public int SubmissionId { get; set; }
    public string? UserName { get; set; }
    public string? ReviewerName { get; set; }
    public string? ChallengeName { get; set; }
    public string? Answer { get; set; }
    public int PenaltyScore { get; set; }
}
