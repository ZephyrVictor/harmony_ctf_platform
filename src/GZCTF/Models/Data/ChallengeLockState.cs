using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

/// <summary>
/// 临时全题锁状态（per-participation per-challenge）
/// </summary>
[Index(nameof(ParticipationId), nameof(ChallengeId))]
[Index(nameof(UnlocksAt))]
public class ChallengeLockState
{
    [Key]
    public int Id { get; set; }

    public int ParticipationId { get; set; }
    public int ChallengeId { get; set; }

    public int TriggeringLevel { get; set; }
    public Guid? TriggeringTrackId { get; set; }

    [MaxLength(32)]
    public string TriggerReason { get; set; } = string.Empty;

    [MaxLength(Limits.MaxFlagLength)]
    public string? TriggerAnswer { get; set; }

    public DateTimeOffset LockedAt { get; set; }
    public DateTimeOffset UnlocksAt { get; set; }

    [NotMapped]
    public bool IsActive => DateTimeOffset.UtcNow < UnlocksAt;

    #region Db Relationship

    public Participation Participation { get; set; } = null!;

    #endregion
}
