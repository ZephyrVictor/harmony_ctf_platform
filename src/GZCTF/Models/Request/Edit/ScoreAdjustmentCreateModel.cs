using GZCTF.Models.Data;

namespace GZCTF.Models.Request.Edit;

/// <summary>
/// 创建调分记录请求
/// </summary>
public class ScoreAdjustmentCreateModel
{
    /// <summary>队伍 ID</summary>
    public int TeamId { get; set; }

    /// <summary>分值变化：正=加分，负=扣分</summary>
    public int Delta { get; set; }

    /// <summary>理由（会写入 GameNotice 通报）</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>类别：Manual 手动调分 / ZeroDayBonus 0day 加分</summary>
    public ScoreAdjustmentCategory Category { get; set; } = ScoreAdjustmentCategory.Manual;

    /// <summary>0day 加分级别。ZeroDayBonus 必填；后端按比赛配置计算分值</summary>
    public ZeroDaySeverity? ZeroDaySeverity { get; set; }

    /// <summary>附加信息（0day 用：CVE 编号等）</summary>
    public string? RelatedInfo { get; set; }
}
