using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

[Index(nameof(SubmissionId), IsUnique = true)]
[Index(nameof(ParticipationId), nameof(ChallengeId), nameof(TrackId), nameof(Level))]
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
    /// Denormalized game ID
    /// </summary>
    public int? GameId { get; set; }

    /// <summary>
    /// Denormalized challenge ID
    /// </summary>
    public int? ChallengeId { get; set; }

    /// <summary>
    /// Denormalized participation ID
    /// </summary>
    public int? ParticipationId { get; set; }

    /// <summary>
    /// Denormalized level
    /// </summary>
    public int? Level { get; set; }

    /// <summary>
    /// Denormalized track ID
    /// </summary>
    public Guid? TrackId { get; set; }

    /// <summary>
    /// 提交类型：Correct | Wrong | Trap | Ban | Duplicate
    /// </summary>
    [MaxLength(20)]
    public string? SubmissionType { get; set; }

    #region Db Relationship

    public int SubmissionId { get; set; }
    public Submission? Submission { get; set; }

    public Guid? MatchedFlagId { get; set; }
    public ChallengeFlag? MatchedFlag { get; set; }

    #endregion
}
