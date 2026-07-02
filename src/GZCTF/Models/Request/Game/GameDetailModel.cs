using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GZCTF.Models.Request.Game;

public class GameDetailModel
{
    /// <summary>
    /// Challenge information
    /// </summary>
    public Dictionary<ChallengeCategory, IEnumerable<ChallengeInfo>> Challenges { get; set; } = new();

    /// <summary>
    /// Number of challenges
    /// </summary>
    public int ChallengeCount { get; set; }

    /// <summary>
    /// Scoreboard information
    /// </summary>
    [JsonPropertyName("rank")]
    public ScoreboardItem? ScoreboardItem { get; set; }

    /// <summary>
    /// Team token
    /// </summary>
    [Required]
    public string TeamToken { get; set; } = string.Empty;

    /// <summary>
    /// Whether writeup submission is required
    /// </summary>
    [Required]
    public bool WriteupRequired { get; set; } = false;

    /// <summary>
    /// Writeup submission deadline
    /// </summary>
    [Required]
    public DateTimeOffset WriteupDeadline { get; set; } = DateTimeOffset.FromUnixTimeSeconds(0);

    /// <summary>
    /// 0day 提交说明 markdown 文案（ZeroDayInfo tab 渲染用）
    /// </summary>
    public string ZeroDayInfoMarkdown { get; set; } = string.Empty;

    /// <summary>
    /// 0day 加分基础倍数
    /// </summary>
    public int ZeroDayBaseScore { get; set; } = 1;

    /// <summary>
    /// 题目冷却分钟数。0 = 不限流
    /// </summary>
    public int CooldownMinutes { get; set; } = 5;
}
