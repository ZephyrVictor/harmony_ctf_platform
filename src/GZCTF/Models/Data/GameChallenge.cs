using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using GZCTF.Models.Request.Edit;

namespace GZCTF.Models.Data;

[JsonConverter(typeof(JsonStringEnumConverter<SubmissionControlLimitMode>))]
public enum SubmissionControlLimitMode
{
    InheritDefault,
    Custom,
    Unlimited
}

public class GameChallenge : Challenge
{
    /// <summary>
    /// Whether to record traffic
    /// </summary>
    public bool EnableTrafficCapture { get; set; }

    /// <summary>
    /// Whether to disable blood bonus
    /// </summary>
    public bool DisableBloodBonus { get; set; }

    /// <summary>
    /// Initial score
    /// </summary>
    [Required]
    public int OriginalScore { get; set; } = 1000;

    /// <summary>
    /// Minimum score rate
    /// </summary>
    [Required]
    [Range(0, 1)]
    public double MinScoreRate { get; set; } = 0.25;

    /// <summary>
    /// Difficulty coefficient
    /// </summary>
    [Required]
    public double Difficulty { get; set; } = 5;

    /// <summary>
    /// 代码仓库（作为比赛题目方向分组依据，如 "arkcompiler_ets_runtime"）
    /// </summary>
    [MaxLength(128)]
    public string? Repository { get; set; }

    /// <summary>
    /// 每队每日本题提交上限自定义值。模式为 Custom 时生效。
    /// </summary>
    public int DailyTeamSubmissionLimit { get; set; }

    /// <summary>
    /// 每队每日本题提交上限模式。
    /// </summary>
    public SubmissionControlLimitMode DailyTeamSubmissionLimitMode { get; set; } =
        SubmissionControlLimitMode.InheritDefault;

    /// <summary>
    /// 每队本题累计提交上限自定义值。模式为 Custom 时生效。
    /// </summary>
    public int TotalTeamSubmissionLimit { get; set; }

    /// <summary>
    /// 每队本题累计提交上限模式。
    /// </summary>
    public SubmissionControlLimitMode TotalTeamSubmissionLimitMode { get; set; } =
        SubmissionControlLimitMode.InheritDefault;

    /// <summary>
    /// 本题错答冷却阈值自定义值。模式为 Custom 时生效。
    /// </summary>
    public int CooldownWrongAnswerLimitOverride { get; set; }

    /// <summary>
    /// 本题错答冷却阈值模式。
    /// </summary>
    public SubmissionControlLimitMode CooldownWrongAnswerLimitMode { get; set; } =
        SubmissionControlLimitMode.InheritDefault;

    /// <summary>
    /// Current score of the challenge
    /// </summary>
    [NotMapped]
    public int CurrentScore => CalculateChallengeScore(
        OriginalScore,
        MinScoreRate,
        Difficulty,
        FirstSolves?.Count ?? 0);


    internal static int CalculateChallengeScore(int originalScore, double minScoreRate, double difficulty,
        int acceptedCount)
    {
        if (acceptedCount <= 1)
            return originalScore;

        return (int)Math.Floor(
            originalScore *
            (minScoreRate + (1.0 - minScoreRate) * Math.Exp((1 - acceptedCount) / difficulty)));
    }

    internal void Update(ChallengeUpdateModel model)
    {
        Title = model.Title ?? Title;
        Content = model.Content ?? Content;
        Category = model.Category ?? Category;
        Hints = model.Hints ?? Hints;
        CPUCount = model.CPUCount ?? CPUCount;
        MemoryLimit = model.MemoryLimit ?? MemoryLimit;
        StorageLimit = model.StorageLimit ?? StorageLimit;
        ContainerImage = model.ContainerImage?.Trim() ?? ContainerImage;
        ExposePort = model.ExposePort ?? ExposePort;
        NetworkMode = model.NetworkMode ?? NetworkMode;
        OriginalScore = model.OriginalScore ?? OriginalScore;
        MinScoreRate = model.MinScoreRate ?? MinScoreRate;
        Difficulty = model.Difficulty ?? Difficulty;
        Repository = model.Repository ?? Repository;
        DailyTeamSubmissionLimit = model.DailyTeamSubmissionLimit ?? DailyTeamSubmissionLimit;
        TotalTeamSubmissionLimit = model.TotalTeamSubmissionLimit ?? TotalTeamSubmissionLimit;
        CooldownWrongAnswerLimitOverride =
            model.CooldownWrongAnswerLimitOverride ?? CooldownWrongAnswerLimitOverride;
        DailyTeamSubmissionLimitMode = model.DailyTeamSubmissionLimitMode ?? DailyTeamSubmissionLimitMode;
        TotalTeamSubmissionLimitMode = model.TotalTeamSubmissionLimitMode ?? TotalTeamSubmissionLimitMode;
        CooldownWrongAnswerLimitMode = model.CooldownWrongAnswerLimitMode ?? CooldownWrongAnswerLimitMode;
        FileName = model.FileName ?? FileName;
        DisableBloodBonus = model.DisableBloodBonus ?? DisableBloodBonus;
        LockDurationMinutes = model.LockDurationMinutes ?? LockDurationMinutes;

        // isEnabled should be updated alone
        IsEnabled = model.IsEnabled ?? IsEnabled;

        // only set DeadlineUtc to null when pass DateTimeOffset.MinValue (but not null)
        if (model.DeadlineUtc is { } time)
            DeadlineUtc = time.ToUnixTimeSeconds() == 0 ? null : time;

        // only set FlagTemplate to null when pass an empty string (but not null)
        if (model.FlagTemplate is { } template)
            FlagTemplate = string.IsNullOrWhiteSpace(template) ? null : template;

        // Container only
        EnableTrafficCapture = Type.IsContainer() && (model.EnableTrafficCapture ?? EnableTrafficCapture);
    }

    #region Db Relationship

    /// <summary>
    /// Submissions
    /// </summary>
    public List<Submission> Submissions { get; set; } = [];

    /// <summary>
    /// Challenge instances
    /// </summary>
    public List<GameInstance> Instances { get; set; } = [];

    /// <summary>
    /// Teams that activated the challenge
    /// </summary>
    public HashSet<Participation> Teams { get; set; } = [];

    /// <summary>
    /// Configurations for divisions
    /// </summary>
    public HashSet<DivisionChallengeConfig> DivisionConfigs { get; set; } = [];

    /// <summary>
    /// First solves recorded for this challenge.
    /// </summary>
    public List<FirstSolve>? FirstSolves { get; set; } = [];

    /// <summary>
    /// 多级 flag Track 列表（每个 Track 对应一个漏洞实例）
    /// </summary>
    public List<ChallengeTrack> Tracks { get; set; } = [];

    /// <summary>
    /// 多级 flag LevelConfig 列表（所有 Track 共享）
    /// </summary>
    public List<ChallengeLevelConfig> LevelConfigs { get; set; } = [];

    /// <summary>
    /// Game ID
    /// </summary>
    public int GameId { get; set; }

    /// <summary>
    /// Game object
    /// </summary>
    public Game Game { get; set; } = null!;

    #endregion Db Relationship
}
