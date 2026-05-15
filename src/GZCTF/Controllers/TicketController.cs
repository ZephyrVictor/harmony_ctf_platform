using System.Net.Mime;
using System.Security.Claims;
using GZCTF.Middlewares;
using GZCTF.Models.Request;
using GZCTF.Repositories.Interface;
using GZCTF.Services.Cache;
using GZCTF.Services.Scoring;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;

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
    IBlobRepository blobRepository,
    ScoringService scoringService,
    CompetitionSettingsService settingsService,
    CacheHelper cacheHelper) : ControllerBase
{
    const int MaxAttachmentCount = 5;
    const long MaxAttachmentSize = 10 * 1024 * 1024;

    static readonly IReadOnlyDictionary<string, AttachmentImageFormat> AllowedAttachmentImageFormats =
        new Dictionary<string, AttachmentImageFormat>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/png"] = new("image/png", "png"),
            ["image/jpeg"] = new("image/jpeg", "jpg"),
            ["image/gif"] = new("image/gif", "gif"),
            ["image/webp"] = new("image/webp", "webp")
        };

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

        var record = await recordRepository.GetBySubmissionId(model.SubmissionId, token);
        if (record?.Submission is null)
            return BadRequest(new RequestResponse("Submission not found"));

        var currentUserId = Guid.Parse(userId);
        if (!SubmissionBelongsToUser(record.Submission, currentUserId))
            return Forbid();

        if (!record.IsPenalty)
            return BadRequest(new RequestResponse("Only penalty submissions can be appealed"));

        if (model.GameId.HasValue && record.Submission.GameId != model.GameId.Value)
            return BadRequest(new RequestResponse("Submission does not belong to the specified game"));

        var maxTickets = await settingsService.Get<int>("Ticket.MaxTicketsPerSubmission", token);
        var existingCount = await ticketRepository.CountBySubmission(model.SubmissionId, token);
        if (existingCount >= maxTickets)
            return Conflict(new RequestResponse($"Maximum of {maxTickets} ticket(s) per submission exceeded"));

        var ticket = new Ticket
        {
            Description = model.Description,
            SubmissionId = model.SubmissionId,
            UserId = currentUserId
        };

        ticket = await ticketRepository.CreateTicket(ticket, token);
        return CreatedAtAction(nameof(GetTicket), new { ticketId = ticket.Id }, ToDto(ticket));
    }

    /// <summary>
    /// Upload image attachments to a pending ticket
    /// </summary>
    [RequireUser]
    [HttpPost("{ticketId:guid}/attachments")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(TicketAttachmentDto[]), StatusCodes.Status201Created)]
    [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = MaxAttachmentCount * MaxAttachmentSize)]
    public async Task<IActionResult> UploadAttachments(
        [FromRoute] Guid ticketId,
        [FromForm] List<IFormFile>? files,
        CancellationToken token)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized(new RequestResponse("User not found"));

        var ticket = await ticketRepository.GetTicket(ticketId, token);
        if (ticket is null)
            return NotFound(new RequestResponse("Ticket not found"));

        var currentUserId = Guid.Parse(userId);
        if (ticket.UserId != currentUserId)
            return Forbid();

        if (ticket.Status != TicketStatus.Pending)
            return BadRequest(new RequestResponse("Only pending tickets can accept attachments"));

        if (ticket.Submission is null || !SubmissionBelongsToUser(ticket.Submission, currentUserId))
            return Forbid();

        if (files is null || files.Count == 0)
            return BadRequest(new RequestResponse("At least one attachment is required"));

        List<ValidatedAttachmentImage> validatedFiles = [];
        foreach (var file in files)
        {
            if (file.Length <= 0)
            {
                await DisposeValidatedAttachmentImages(validatedFiles);
                return BadRequest(new RequestResponse("Attachment is empty"));
            }

            if (file.Length > MaxAttachmentSize)
            {
                await DisposeValidatedAttachmentImages(validatedFiles);
                return BadRequest(new RequestResponse($"Attachment exceeds {MaxAttachmentSize / 1024 / 1024} MiB"));
            }

            var validated = await ValidateAttachmentImage(file, token);
            if (validated is null)
            {
                await DisposeValidatedAttachmentImages(validatedFiles);
                return BadRequest(new RequestResponse("Only png, jpeg, gif, or webp raster image attachments are supported"));
            }

            validatedFiles.Add(validated);
        }

        List<TicketAttachment> attachments = [];
        try
        {
            foreach (var validated in validatedFiles)
            {
                var attachmentId = Guid.NewGuid();
                var localFile = await blobRepository.CreateOrUpdateBlobFromStream(
                    $"{attachmentId:N}.{validated.Extension}", validated.Content, token);

                attachments.Add(new TicketAttachment
                {
                    Id = attachmentId,
                    LocalFileId = localFile.Id,
                    LocalFile = localFile,
                    UploaderId = currentUserId,
                    ContentType = validated.ContentType
                });
            }

            var result = await ticketRepository.AddAttachments(ticketId, attachments, MaxAttachmentCount, token);
            if (result is null)
            {
                foreach (var attachment in attachments)
                {
                    if (attachment.LocalFile is not null)
                        await blobRepository.DeleteBlob(attachment.LocalFile, token);
                }

                return BadRequest(new RequestResponse($"Maximum of {MaxAttachmentCount} attachment(s) per ticket exceeded"));
            }

            return CreatedAtAction(nameof(GetTicket), new { ticketId }, result.Select(ToDto).ToArray());
        }
        finally
        {
            await DisposeValidatedAttachmentImages(validatedFiles);
        }
    }

    /// <summary>
    /// Get current user's tickets
    /// </summary>
    [RequireUser]
    [HttpGet("mine")]
    [ProducesResponseType(typeof(TicketDto[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyTickets(
        [FromQuery] int? gameId,
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
        [FromQuery] int? gameId = null,
        [FromQuery] int count = 100,
        [FromQuery] int skip = 0,
        CancellationToken token = default)
    {
        var tickets = await ticketRepository.GetAllTickets(status, count, skip, gameId, token);
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

        if (ticket.Submission is not null)
            await cacheHelper.FlushScoreboardCache(ticket.Submission.GameId, token);

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
            GameId = ticket.Submission?.GameId ?? 0,
            UserName = ticket.User?.UserName,
            ReviewerName = ticket.Reviewer?.UserName,
            ChallengeName = ticket.Submission?.GameChallenge?.Title,
            Answer = ticket.Submission?.Answer,
            PenaltyScore = ticket.Submission?.SubmissionRecord?.IsPenalty == true
                ? Math.Abs(ticket.Submission.SubmissionRecord.ScoreChange)
                : 0,
            Attachments = ticket.Attachments
                .OrderBy(a => a.CreatedAt)
                .Select(ToDto)
                .ToArray()
        };
    }

    private static TicketAttachmentDto ToDto(TicketAttachment attachment)
    {
        return new TicketAttachmentDto
        {
            Id = attachment.Id,
            FileName = attachment.LocalFile?.Name ?? string.Empty,
            FileSize = attachment.LocalFile?.FileSize ?? 0,
            Url = attachment.LocalFile?.Url(),
            ContentType = attachment.ContentType,
            CreatedAt = attachment.CreatedAt
        };
    }

    private static bool SubmissionBelongsToUser(Submission submission, Guid userId) =>
        submission.UserId == userId ||
        submission.Participation.Members.Any(m => m.UserId == userId);

    private static async Task<ValidatedAttachmentImage?> ValidateAttachmentImage(
        IFormFile file,
        CancellationToken token)
    {
        var content = BufferHelper.GetTempStream(file.Length);

        try
        {
            await file.CopyToAsync(content, token);
            content.Position = 0;

            using var image = await Image.LoadAsync(content, token);
            var format = image.Metadata.DecodedImageFormat;
            if (format is null || !TryGetAttachmentImageFormat(format, out var imageFormat))
            {
                await content.DisposeAsync();
                return null;
            }

            content.Position = 0;
            return new(content, imageFormat.ContentType, imageFormat.Extension);
        }
        catch (Exception) when (!token.IsCancellationRequested)
        {
            await content.DisposeAsync();
            return null;
        }
    }

    private static bool TryGetAttachmentImageFormat(IImageFormat format, out AttachmentImageFormat imageFormat)
    {
        if (AllowedAttachmentImageFormats.TryGetValue(format.DefaultMimeType, out var knownFormat))
        {
            imageFormat = knownFormat;
            return true;
        }

        imageFormat = null!;
        return false;
    }

    private static async Task DisposeValidatedAttachmentImages(IEnumerable<ValidatedAttachmentImage> attachments)
    {
        foreach (var attachment in attachments)
            await attachment.DisposeAsync();
    }

    private sealed record AttachmentImageFormat(string ContentType, string Extension);

    private sealed record ValidatedAttachmentImage(Stream Content, string ContentType, string Extension)
        : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => Content.DisposeAsync();
    }
}

public class TicketCreateModel
{
    public int SubmissionId { get; set; }
    public int? GameId { get; set; }
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
    public int GameId { get; set; }
    public string? UserName { get; set; }
    public string? ReviewerName { get; set; }
    public string? ChallengeName { get; set; }
    public string? Answer { get; set; }
    public int PenaltyScore { get; set; }
    public TicketAttachmentDto[] Attachments { get; set; } = [];
}

public class TicketAttachmentDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? Url { get; set; }
    public string? ContentType { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
