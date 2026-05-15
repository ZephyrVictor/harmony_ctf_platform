using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

[Index(nameof(SubmissionId), IsUnique = true)]
public class SubmissionRecord
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Score change (positive = correct flag, negative = penalty)
    /// </summary>
    public int ScoreChange { get; set; }

    /// <summary>
    /// Whether this record represents a penalty (wrong answer)
    /// </summary>
    public bool IsPenalty { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Denormalized game ID for scoped filtering and HybridLine score uniqueness
    /// </summary>
    public int? GameId { get; set; }

    /// <summary>
    /// Denormalized challenge ID for scoped filtering and HybridLine score uniqueness
    /// </summary>
    public int? ChallengeId { get; set; }

    /// <summary>
    /// Denormalized participation ID for scoped filtering and HybridLine score uniqueness
    /// </summary>
    public int? ParticipationId { get; set; }

    #region Db Relationship

    /// <summary>
    /// Original submission ID
    /// </summary>
    public int SubmissionId { get; set; }

    /// <summary>
    /// Original submission
    /// </summary>
    public Submission? Submission { get; set; }

    /// <summary>
    /// Matched flag ID (null if penalty)
    /// </summary>
    public Guid? MatchedFlagId { get; set; }

    /// <summary>
    /// Matched flag
    /// </summary>
    public ChallengeFlag? MatchedFlag { get; set; }

    #endregion Db Relationship
}
