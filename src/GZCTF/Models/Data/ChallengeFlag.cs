using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

[Index(nameof(ChallengeId))]
public class ChallengeFlag
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Flag value
    /// </summary>
    [Required]
    [MaxLength(Limits.MaxFlagLength)]
    public string FlagValue { get; set; } = string.Empty;

    /// <summary>
    /// Independent score for this flag
    /// </summary>
    [Required]
    [Range(1, int.MaxValue)]
    public int Score { get; set; } = 100;

    /// <summary>
    /// Sort order for display
    /// </summary>
    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// Whether this flag has been assigned to a participant (for dynamic attachment dispatch)
    /// </summary>
    public bool IsOccupied { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    #region Db Relationship

    /// <summary>
    /// Challenge ID
    /// </summary>
    public int ChallengeId { get; set; }

    /// <summary>
    /// Challenge
    /// </summary>
    public GameChallenge? Challenge { get; set; }

    /// <summary>
    /// Exercise ID (for exercise challenges)
    /// </summary>
    public int? ExerciseId { get; set; }

    /// <summary>
    /// Exercise challenge
    /// </summary>
    public ExerciseChallenge? Exercise { get; set; }

    /// <summary>
    /// Per-flag attachment (for dynamic attachment challenges)
    /// </summary>
    public int? AttachmentId { get; set; }

    /// <summary>
    /// Attachment
    /// </summary>
    public Attachment? Attachment { get; set; }

    /// <summary>
    /// Submission records that matched this flag
    /// </summary>
    public List<SubmissionRecord> SubmissionRecords { get; set; } = [];

    #endregion Db Relationship
}
