using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Request.Game;

/// <summary>
/// Flag submission response (D6)
/// </summary>
public class FlagSubmitResponse
{
    /// <summary>
    /// Submission ID
    /// </summary>
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// Result message (e.g., "Accepted", "Wrong answer", "Trap answer triggered")
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Matched Track ID (D6: L1 response returns matched trackId for L2+ submissions)
    /// </summary>
    public Guid? TrackId { get; set; }

    /// <summary>
    /// Matched Level (D6: current level that was submitted)
    /// </summary>
    public int? Level { get; set; }

    /// <summary>
    /// 当前题目下已解漏洞数（按 Track 计数，L1+L2+L3 全解才算 1 个）
    /// </summary>
    public int? SolvedCount { get; set; }

    /// <summary>
    /// 当前活跃 TrackId（= TrackId 字段的显式别名，供前端刷新后恢复）
    /// </summary>
    public Guid? ActiveTrackId { get; set; }

    /// <summary>
    /// 该 Track 的最高 Level 是否已解（提示选手"该漏洞已完整解决"）
    /// </summary>
    public bool? IsTrackComplete { get; set; }

    /// <summary>
    /// 是否触发临时全题锁
    /// </summary>
    public bool ChallengeLocked { get; set; }

    /// <summary>
    /// 全题锁解锁时间
    /// </summary>
    public DateTimeOffset? UnlocksAt { get; set; }

    /// <summary>
    /// 触发封禁的 TrackId
    /// </summary>
    public Guid? BannedTrackId { get; set; }

    /// <summary>
    /// 触发封禁的 Level
    /// </summary>
    public int? BannedLevel { get; set; }

    /// <summary>
    /// 触发锁/封禁的错误答案（用于详细提示）
    /// </summary>
    public string? TriggerAnswer { get; set; }

    /// <summary>
    /// 题目冷却信息（per-challenge 答错次数 + 临时冷却）
    /// </summary>
    public CooldownInfo? Cooldown { get; set; }

    /// <summary>
    /// 当前题目提交控制状态（配额 + 冷却）
    /// </summary>
    public ChallengeSubmissionControlStatus? SubmissionControl { get; set; }
}
