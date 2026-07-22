using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Models.Data;

[Index(nameof(GameId))]
[Index(nameof(TeamId))]
[Index(nameof(Category))]
[Index(nameof(CreatedAt))]
public class ScoreAdjustment
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public int GameId { get; set; }

    [JsonIgnore]
    public Game Game { get; set; } = null!;

    [Required]
    public int TeamId { get; set; }

    [JsonIgnore]
    public Team Team { get; set; } = null!;

    /// <summary>
    /// 分值变化：正数=加分，负数=扣分
    /// </summary>
    [Required]
    public int Delta { get; set; }

    [Required]
    [MaxLength(512)]
    public string Reason { get; set; } = string.Empty;

    [Required]
    public ScoreAdjustmentCategory Category { get; set; } = ScoreAdjustmentCategory.Manual;

    /// <summary>
    /// 0day 加分级别，非 0day 记录为空
    /// </summary>
    public ZeroDaySeverity? ZeroDaySeverity { get; set; }

    /// <summary>
    /// 0day 加分用：CVE 编号等附加信息
    /// </summary>
    [MaxLength(256)]
    public string? RelatedInfo { get; set; }

    [Required]
    public Guid OperatorId { get; set; }

    [JsonIgnore]
    public UserInfo Operator { get; set; } = null!;

    [Required]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ScoreAdjustmentCategory
{
    Manual,
    ZeroDayBonus
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ZeroDaySeverity
{
    Critical,
    High,
    Medium,
    Low
}
