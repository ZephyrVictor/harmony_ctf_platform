namespace GZCTF.Models.Request.Edit;

/// <summary>
/// 创建手动封禁请求
/// </summary>
public class BanCreateModel
{
    /// <summary>队伍 ID</summary>
    public int TeamId { get; set; }

    /// <summary>题目 ID</summary>
    public int ChallengeId { get; set; }

    /// <summary>Track ID</summary>
    public Guid TrackId { get; set; }

    /// <summary>Level（1/2/3）</summary>
    public int Level { get; set; }

    /// <summary>封禁理由</summary>
    public string Reason { get; set; } = string.Empty;
}
