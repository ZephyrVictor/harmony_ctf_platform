namespace GZCTF.Models.Request.Edit;

/// <summary>
/// 批量更新 flag 请求
/// </summary>
public class BatchFlagUpdateModel
{
    /// <summary>要更新的 flag ID 列表</summary>
    public List<Guid> FlagIds { get; set; } = new();

    /// <summary>目标启用状态</summary>
    public bool IsEnabled { get; set; }
}

/// <summary>
/// 批量删除 flag 请求
/// </summary>
public class BatchFlagDeleteModel
{
    /// <summary>要删除的 flag ID 列表</summary>
    public List<Guid> FlagIds { get; set; } = new();
}
