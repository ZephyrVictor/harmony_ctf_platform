using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

[Index(nameof(UserId))]
[Index(nameof(Status))]
public class Ticket
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Appeal description from the contestant
    /// </summary>
    [Required]
    [MinLength(1)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the ticket
    /// </summary>
    public TicketStatus Status { get; set; } = TicketStatus.Pending;

    /// <summary>
    /// Review comment from the admin
    /// </summary>
    [MaxLength(500)]
    public string? ReviewComment { get; set; }

    /// <summary>
    /// Refund score (returned penalty). Only set on approval.
    /// </summary>
    public int RefundScore { get; set; }

    /// <summary>
    /// Bonus score (extra compensation). Only set on approval.
    /// </summary>
    public int BonusScore { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReviewedAt { get; set; }

    #region Db Relationship

    /// <summary>
    /// Submission ID this ticket is linked to (must be a penalty submission)
    /// </summary>
    public int SubmissionId { get; set; }

    /// <summary>
    /// Submission
    /// </summary>
    public Submission? Submission { get; set; }

    /// <summary>
    /// Contestant who created the ticket
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Contestant
    /// </summary>
    public UserInfo? User { get; set; }

    /// <summary>
    /// Admin who reviewed the ticket
    /// </summary>
    public Guid? ReviewerId { get; set; }

    /// <summary>
    /// Reviewer
    /// </summary>
    public UserInfo? Reviewer { get; set; }

    #endregion Db Relationship
}

public enum TicketStatus
{
    Pending,
    Approved,
    Rejected
}
