using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

/// <summary>
/// 永久 (Track, Level) 封禁状态（per-participation）
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
}
