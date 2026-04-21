using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

[Index(nameof(ChallengeId), IsUnique = true)]
public class ChallengePenalty
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Score deducted for each wrong submission. 0 means no penalty.
    /// </summary>
    [Required]
    [Range(0, int.MaxValue)]
    public int PenaltyScore { get; set; }

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

    #endregion Db Relationship
}
