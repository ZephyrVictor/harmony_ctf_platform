namespace GZCTF.Models.Request.Game;

/// <summary>
/// 题目冷却状态快照（返回给前端）
/// </summary>
public class CooldownInfo
{
    /// <summary>
    /// 当前周期错答次数
    /// </summary>
    public int WrongCount { get; set; }

    /// <summary>
    /// 错答次数阈值（= trackCount × 5）。0 = 不限流
    /// </summary>
    public int WrongAnswerLimit { get; set; }

    /// <summary>
    /// 冷却分钟数（per-game 配置）
    /// </summary>
    public int CooldownMinutes { get; set; }

    /// <summary>
    /// 冷却到期时间。null = 未冷却；&gt; now = 冷却中
    /// </summary>
    public DateTimeOffset? CooldownUntil { get; set; }
}
