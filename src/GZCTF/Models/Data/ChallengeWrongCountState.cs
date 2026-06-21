using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

/// <summary>
/// per-Level 错误提交计数（锁到期后重置）
/// </summary>
[Index(nameof(ParticipationId), nameof(ChallengeId), nameof(Level), IsUnique = true)]
public class ChallengeWrongCountState
{
    [Key]
    public int Id { get; set; }

    public int ParticipationId { get; set; }
    public int ChallengeId { get; set; }
    public int Level { get; set; }

    public int WrongCount { get; set; }
    public DateTimeOffset LastResetAt { get; set; } = DateTimeOffset.UtcNow;
}
