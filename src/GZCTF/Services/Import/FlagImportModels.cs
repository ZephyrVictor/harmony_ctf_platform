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
    /// FilePath | FlawNo | Title
    /// </summary>
    public string MatchBy { get; set; } = "FilePath";

    public List<LevelColumnMapping> LevelColumns { get; set; } = [];
}

public class LevelColumnMapping
{
    public int Level { get; set; }
    public string ColumnName { get; set; } = string.Empty;
    public ChallengeFlagAnswerType AnswerType { get; set; } = ChallengeFlagAnswerType.Exact;
    public int Score { get; set; }
    public int SubmissionLimit { get; set; }
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
}

public class FlagImportResult
{
    public int TotalRows { get; set; }
    public int Imported { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
    public int Failed { get; set; }
    public List<FlagImportError> Errors { get; set; } = [];
}

public class FlagImportError
{
    public int Row { get; set; }
    public string Error { get; set; } = string.Empty;
}
