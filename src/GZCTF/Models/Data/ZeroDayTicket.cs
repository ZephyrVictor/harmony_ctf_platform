using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

[Index(nameof(UserId))]
[Index(nameof(Status))]
[Index(nameof(GameId))]
public class ZeroDayTicket
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Title { get; set; }

    public ZeroDayTicketStatus Status { get; set; } = ZeroDayTicketStatus.Pending;

    [MaxLength(500)]
    public string? ReviewComment { get; set; }

    public int BonusScore { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReviewedAt { get; set; }

    /// <summary>
    /// 严重度等级（决定加分梯度：Critical N×100, High N×60, Medium N×30, Low N×10）
    /// </summary>
    public ZeroDaySeverity Severity { get; set; } = ZeroDaySeverity.None;

    /// <summary>
    /// 模块/子模块路径（用于关联到 GameChallenge）
    /// </summary>
    [MaxLength(256)]
    public string? Module { get; set; }

    /// <summary>
    /// 文件路径（L1 flag 主键）
    /// </summary>
    [MaxLength(512)]
    public string? FilePath { get; set; }

    /// <summary>
    /// 行号或行范围（如 "c:1060", "c:550-559", "c:281、c:286"）
    /// </summary>
    [MaxLength(128)]
    public string? LineNumber { get; set; }

    /// <summary>
    /// 修复 commit hash (L2 flag)
    /// </summary>
    [MaxLength(64)]
    public string? CommitHash { get; set; }

    /// <summary>
    /// CWE 分类（L3 flag, 多个用 "; " 分隔，如 "CWE-125; CWE-787"）
    /// </summary>
    [MaxLength(128)]
    public string? CWE { get; set; }

    /// <summary>
    /// PoC 或佐证材料（URL/文本）
    /// </summary>
    [MaxLength(4000)]
    public string? PoC { get; set; }

    /// <summary>
    /// 审核通过后系统创建的 TrackId（用于闭环追踪）
    /// </summary>
    public Guid? CreatedTrackId { get; set; }

    #region Db Relationship

    public int GameId { get; set; }
    public Game? Game { get; set; }

    public int? ChallengeId { get; set; }
    public GameChallenge? Challenge { get; set; }

    public Guid UserId { get; set; }
    public UserInfo? User { get; set; }

    public Guid? ReviewerId { get; set; }
    public UserInfo? Reviewer { get; set; }

    #endregion Db Relationship
}

[JsonConverter(typeof(JsonStringEnumConverter<ZeroDayTicketStatus>))]
public enum ZeroDayTicketStatus
{
    Pending,
    Approved,
    Rejected
}

[JsonConverter(typeof(JsonStringEnumConverter<ZeroDaySeverity>))]
public enum ZeroDaySeverity
{
    None,
    Low,
    Medium,
    High,
    Critical
}
