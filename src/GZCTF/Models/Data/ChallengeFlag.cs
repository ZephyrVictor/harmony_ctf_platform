using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

[Index(nameof(ChallengeId))]
public class ChallengeFlag
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Flag value (stored as-is, consistent with FlagContext)
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
    /// Submission records that matched this flag
    /// </summary>
    public List<SubmissionRecord> SubmissionRecords { get; set; } = [];

    #endregion Db Relationship
}
