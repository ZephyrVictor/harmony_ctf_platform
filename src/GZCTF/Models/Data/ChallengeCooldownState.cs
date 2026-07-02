using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

/// <summary>
/// per-(participation, challenge) 答错计数与冷却状态。
/// 答错达阈值（trackCount×5）后进入冷却，<see cref="CooldownUntil"/> 之前禁止提交。
/// 冷却到期后自动解封，WrongCount 重置为 0。
/// </summary>
[Index(nameof(ParticipationId), nameof(ChallengeId), IsUnique = true)]
public class ChallengeCooldownState
{
    [Key]
    public int Id { get; set; }

    public int ParticipationId { get; set; }

    [JsonIgnore]
    public Participation Participation { get; set; } = null!;

    public int ChallengeId { get; set; }

    [JsonIgnore]
    public GameChallenge Challenge { get; set; } = null!;

    /// <summary>
    /// 当前周期错答次数。达阈值后重置为 0 并进入冷却。
    /// </summary>
    public int WrongCount { get; set; }

    /// <summary>
    /// 冷却到期时间。null = 未冷却；&gt; now = 冷却中。
    /// </summary>
    public DateTimeOffset? CooldownUntil { get; set; }

    public DateTimeOffset LastWrongAt { get; set; } = DateTimeOffset.UtcNow;
}
