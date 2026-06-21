using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

[Index(nameof(ChallengeId))]
[Index(nameof(ChallengeId), nameof(Level), nameof(TrackId))]
public class ChallengeFlag
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Flag value（匹配用）
    /// </summary>
    [Required]
    [MaxLength(Limits.MaxFlagLength)]
    public string FlagValue { get; set; } = string.Empty;

    /// <summary>
    /// Per-flag 答案类型，决定匹配算法
    /// </summary>
    public ChallengeFlagAnswerType AnswerType { get; set; } = ChallengeFlagAnswerType.Exact;

    /// <summary>
    /// 原始答案文本（归一化前）
    /// </summary>
    [MaxLength(Limits.MaxFlagLength)]
    public string RawAnswer { get; set; } = string.Empty;

    /// <summary>
    /// 归一化后的匹配键
    /// </summary>
    [MaxLength(Limits.MaxFlagLength + 16)]
    public string NormalizedAnswerKey { get; set; } = string.Empty;

    /// <summary>
    /// 是否参与匹配
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 是否对选手可见
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// 是否已被分配（dynamic attachment 用）
    /// </summary>
    public bool IsOccupied { get; set; }

    /// <summary>
    /// 显示顺序
    /// </summary>
    public int SortOrder { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// 所属层级（1, 2, 3...）
    /// </summary>
    public int Level { get; set; } = 1;

    /// <summary>
    /// 所属 Track（同 TrackId 的 flags 属于同一漏洞实例）
    /// </summary>
    public Guid TrackId { get; set; } = Guid.NewGuid();

    #region Db Relationship

    public int ChallengeId { get; set; }
    public GameChallenge? Challenge { get; set; }

    public int? ExerciseId { get; set; }
    public ExerciseChallenge? Exercise { get; set; }

    public int? AttachmentId { get; set; }
    public Attachment? Attachment { get; set; }

    public List<SubmissionRecord> SubmissionRecords { get; set; } = [];

    #endregion
}
