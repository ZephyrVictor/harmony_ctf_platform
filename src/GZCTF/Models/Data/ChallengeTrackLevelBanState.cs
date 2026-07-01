using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

/// <summary>
/// 永久 (Track, Level) 封禁状态（per-participation）
/// 包括 trap 自动封禁（IsManual=false）和管理员手动封禁（IsManual=true）
/// </summary>
[Index(nameof(ParticipationId), nameof(ChallengeId), nameof(TrackId), nameof(Level), IsUnique = true)]
public class ChallengeTrackLevelBanState
{
    [Key]
    public int Id { get; set; }

    public int ParticipationId { get; set; }
    public int ChallengeId { get; set; }
    public Guid TrackId { get; set; }
    public int Level { get; set; }

    [MaxLength(Limits.MaxFlagLength)]
    public string BanAnswer { get; set; } = string.Empty;

    public DateTimeOffset BannedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// 封禁理由（trap 自动封为 "Trap triggered"，手动封为管理员填写）
    /// </summary>
    [MaxLength(256)]
    public string? Reason { get; set; }

    /// <summary>
    /// 操作管理员 ID（trap 自动封为 null）
    /// </summary>
    public Guid? BannedBy { get; set; }

    /// <summary>
    /// 是否手动封禁（true=管理员手动，false=trap 自动）
    /// </summary>
    public bool IsManual { get; set; } = false;
}
