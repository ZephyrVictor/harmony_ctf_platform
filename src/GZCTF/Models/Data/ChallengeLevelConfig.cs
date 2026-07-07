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

    /// <summary>
    /// 是否区分答案整体大小写。SourceLocation 下主要用于路径部分。
    /// </summary>
    public bool CaseSensitive { get; set; } = true;

    /// <summary>
    /// SourceLocation 路径部分是否区分大小写。
    /// </summary>
    public bool PathCaseSensitive { get; set; } = true;

    /// <summary>
    /// SourceLocation 行号 OR 分隔符，例如 "、,"。
    /// </summary>
    [MaxLength(32)]
    public string OrSeparators { get; set; } = "、,;";

    /// <summary>
    /// SourceLocation 行号范围分隔符。
    /// </summary>
    [MaxLength(8)]
    public string RangeSeparator { get; set; } = "-";

    /// <summary>
    /// 是否剥离行号中的 c: / C: 前缀。
    /// </summary>
    public bool StripLinePrefix { get; set; } = true;

    public bool IsEnabled { get; set; } = true;
    public int SortOrder { get; set; }

    #region Db Relationship

    public GameChallenge Challenge { get; set; } = null!;

    #endregion
}
