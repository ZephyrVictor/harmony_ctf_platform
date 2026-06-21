using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

/// <summary>
/// 漏洞实例 track（数据集一行 = 一个 Track）
/// </summary>
[Index(nameof(ChallengeId))]
public class ChallengeTrack
{
    [Key]
    public Guid Id { get; set; }

    public int ChallengeId { get; set; }

    [MaxLength(200)]
    public string? Title { get; set; }

    [MaxLength(64)]
    public string? FlawNo { get; set; }

    [MaxLength(128)]
    public string? Repository { get; set; }

    [MaxLength(128)]
    public string? Module { get; set; }

    [MaxLength(512)]
    public string? FilePath { get; set; }

    [MaxLength(128)]
    public string? Category { get; set; }

    [MaxLength(512)]
    public string? ReferenceUrl { get; set; }

    public bool IsEnabled { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    #region Db Relationship

    public GameChallenge Challenge { get; set; } = null!;

    #endregion
}
