using System.Text.Json.Serialization;

namespace GZCTF.Models.Response;

/// <summary>
/// 调分记录 DTO
/// </summary>
public class ScoreAdjustmentDto
{
    public Guid Id { get; set; }
    public int GameId { get; set; }
    public int TeamId { get; set; }
    public int Delta { get; set; }
    public string Reason { get; set; } = string.Empty;
    public ScoreAdjustmentCategory Category { get; set; }
    public ZeroDaySeverity? ZeroDaySeverity { get; set; }
    public string? RelatedInfo { get; set; }
    public Guid OperatorId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// 0day 加分统计
/// </summary>
public class ZeroDayBonusStatsDto
{
    public int TotalScore { get; set; }
    public int Critical { get; set; }
    public int High { get; set; }
    public int Medium { get; set; }
    public int Low { get; set; }
}

/// <summary>
/// 队伍搜索结果 DTO
/// </summary>
public class TeamSearchResultDto
{
    public int TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public string TeamToken { get; set; } = string.Empty;
    public string[] Members { get; set; } = Array.Empty<string>();
    public int NdayScore { get; set; }
    public int ZeroDayNdayScoreRequirement { get; set; }
    public int ZeroDayBaseScore { get; set; }
    public bool CanReceiveZeroDayBonus { get; set; }
}
