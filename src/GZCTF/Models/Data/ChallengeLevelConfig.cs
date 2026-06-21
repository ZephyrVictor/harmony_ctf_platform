using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

/// <summary>
/// Per-challenge per-level 配置（所有 Track 共享）
/// </summary>
[Index(nameof(ChallengeId), nameof(Level), IsUnique = true)]
public class ChallengeLevelConfig
{
    [Key]
    public int Id { get; set; }

    public int ChallengeId { get; set; }

    /// <summary>
    /// 层级编号，1, 2, 3...
    /// </summary>
    public int Level { get; set; }

    [MaxLength(64)]
    public string? Title { get; set; }

    /// <summary>
    /// 本 level 解出分值
    /// </summary>
    [Range(0, int.MaxValue)]
    public int Score { get; set; }

    /// <summary>
    /// 0 = 继承 Challenge.SubmissionLimit
    /// </summary>
    [Range(0, int.MaxValue)]
    public int SubmissionLimit { get; set; }

    /// <summary>
    /// 0 = 继承 ChallengePenalty
    /// </summary>
    [Range(0, int.MaxValue)]
    public int PenaltyScore { get; set; }

    /// <summary>
    /// 触发临时全题锁的 trap 答案
    /// </summary>
    public List<string> LockOnAnswers { get; set; } = [];

    /// <summary>
    /// 触发永久 (Track, Level) 封禁的 trap 答案（仅 Level 2+ 生效）
    /// </summary>
    public List<string> BanOnAnswers { get; set; } = [];

    public bool IsEnabled { get; set; } = true;
    public int SortOrder { get; set; }

    #region Db Relationship

    public GameChallenge Challenge { get; set; } = null!;

    #endregion
}
