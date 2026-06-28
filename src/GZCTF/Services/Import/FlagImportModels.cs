namespace GZCTF.Services.Import;

public class FlagImportColumnMapping
{
    public string? TrackTitleColumn { get; set; }
    public string? TrackFlawNoColumn { get; set; }
    public string? TrackRepositoryColumn { get; set; }
    public string? TrackModuleColumn { get; set; }
    public string? TrackFilePathColumn { get; set; }
    public string? TrackCategoryColumn { get; set; }
    public string? TrackReferenceUrlColumn { get; set; }

    /// <summary>
    /// 用于按 (仓库, 子模块) 自动建题目时的"子模块"列（对应 GameChallenge.Title）。
    /// 若不指定，则导入到单一题目（不自动建题目）。
    /// </summary>
    public string? SubmoduleColumn { get; set; }

    /// <summary>
    /// 用于按 (仓库, 子模块) 自动建题目时的"所属仓库"列（对应 GameChallenge.Repository）。
    /// 与 SubmoduleColumn 配合使用。
    /// </summary>
    public string? RepositoryColumn { get; set; }

    /// <summary>
    /// FilePath | FlawNo | Title
    /// </summary>
    public string MatchBy { get; set; } = "FilePath";

    public List<LevelColumnMapping> LevelColumns { get; set; } = [];
}

public class LevelColumnMapping
{
    public int Level { get; set; }
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>
    /// 额外列，与 ColumnName 用 ":" 拼接成最终 flag 值。
    /// 例如 L1 = 文件路径 + ":" + 代码行数，则 ColumnName="文件路径"，ExtraColumns=["代码行数"]。
    /// </summary>
    public List<string> ExtraColumns { get; set; } = [];

    public ChallengeFlagAnswerType AnswerType { get; set; } = ChallengeFlagAnswerType.Exact;
    public int Score { get; set; }

    /// <summary>
    /// 每条 Track×Level 的最大错误提交次数。0 = 使用默认值 5（规则 R4 防爆破）。
    /// </summary>
    public int SubmissionLimit { get; set; } = 5;

    public int PenaltyScore { get; set; }
    public List<string> LockOnAnswers { get; set; } = [];
    public List<string> BanOnAnswers { get; set; } = [];
    public string? LevelTitle { get; set; }
}

public enum ImportMode { Upsert, Skip, Fail }

public class FlagImportFilePreview
{
    public List<string> ColumnNames { get; set; } = [];
    public List<Dictionary<string, string>> SampleRows { get; set; } = [];

    /// <summary>
    /// 试解析出的 Track 预览（需要传入 mapping 才会填充）
    /// </summary>
    public List<FlagImportTrackPreview> Tracks { get; set; } = [];

    /// <summary>
    /// 数据集中出现的所有仓库名（供前端显示"将创建 N 个新题目方向"）
    /// </summary>
    public List<string> Repositories { get; set; } = [];

    /// <summary>
    /// 数据集中出现的所有子模块名（供前端显示"将创建 N 个新题目"）
    /// </summary>
    public List<string> Submodules { get; set; } = [];
}

public class FlagImportTrackPreview
{
    public string? Title { get; set; }
    public string? FlawNo { get; set; }
    public string? Repository { get; set; }
    public string? Module { get; set; }
    public string? FilePath { get; set; }
    public string? Category { get; set; }
    public string? ReferenceUrl { get; set; }
    public string? Submodule { get; set; }
    public List<FlagImportLevelPreview> Levels { get; set; } = [];
}

public class FlagImportLevelPreview
{
    public int Level { get; set; }
    public string? FlagValue { get; set; }
    public string AnswerType { get; set; } = "Exact";
    public int Score { get; set; }
}

public class FlagImportResult
{
    public int TotalRows { get; set; }
    public int Imported { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
    public int Failed { get; set; }
    public List<FlagImportError> Errors { get; set; } = [];

    /// <summary>
    /// 自动建题目模式下，创建的 GameChallenge 数量
    /// </summary>
    public int CreatedChallenges { get; set; }

    /// <summary>
    /// 自动建题目模式下，复用的已存在 GameChallenge 数量
    /// </summary>
    public int ReusedChallenges { get; set; }
}

public class FlagImportError
{
    public int Row { get; set; }
    public string Error { get; set; } = string.Empty;
}
