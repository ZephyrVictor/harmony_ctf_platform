using GZCTF.Models.Request.Shared;

namespace GZCTF.Models.Request.Game;

/// <summary>
/// Challenge detailed information
/// </summary>
public class ChallengeDetailModel
{
    /// <summary>
    /// Challenge ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Challenge title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Challenge content
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Challenge category
    /// </summary>
    public ChallengeCategory Category { get; set; } = ChallengeCategory.Misc;

    /// <summary>
    /// Challenge hints
    /// </summary>
    public List<string>? Hints { get; set; }

    /// <summary>
    /// Current score of the challenge
    /// </summary>
    public int Score { get; set; }

    /// <summary>
    /// Challenge type
    /// </summary>
    public ChallengeType Type { get; set; } = ChallengeType.StaticAttachment;

    /// <summary>
    /// Flag context
    /// </summary>
    public ClientFlagContext Context { get; set; } = null!;

    /// <summary>
    /// Maximum number of attempts allowed (0 = no limit)
    /// </summary>
    public int Limit { get; set; }

    /// <summary>
    /// 临时全题锁时长（分钟），0 = 永久（只读，展示用）
    /// </summary>
    public int LockDurationMinutes { get; set; }

    /// <summary>
    /// Player-visible flag metadata (legacy HybridLine view; superseded by <see cref="Tracks"/>)
    /// </summary>
    public List<HybridLineFlagInfoModel> Flags { get; set; } = [];

    /// <summary>
    /// Whether all enabled flags have been solved
    /// </summary>
    public bool AllEnabledFlagsSolved { get; set; }

    /// <summary>
    /// Current attempt count
    /// </summary>
    public int Attempts { get; set; }

    /// <summary>
    /// Deadline of the challenge, null means no deadline
    /// </summary>
    public DateTimeOffset? Deadline { get; set; }

    /// <summary>
    /// 多级 flag 状态：每个 Track 的进度
    /// </summary>
    public List<TrackStatusModel>? Tracks { get; set; }

    /// <summary>
    /// 全题锁状态
    /// </summary>
    public ChallengeLockStatusModel? ChallengeLock { get; set; }

    /// <summary>
    /// 当前可提交的最低 Level（前端引导用）
    /// </summary>
    public int CurrentLevel { get; set; } = 1;

    /// <summary>
    /// 代码仓库（作为比赛题目方向分组依据，如 "arkcompiler_ets_runtime"）
    /// </summary>
    public string? Repository { get; set; }

    /// <summary>
    /// 当前活跃 TrackId（最近一次有提交记录的 Track）
    /// </summary>
    public Guid? ActiveTrackId { get; set; }

    /// <summary>
    /// 题目冷却信息（per-challenge 答错次数 + 临时冷却）
    /// </summary>
    public CooldownInfo? Cooldown { get; set; }

    /// <summary>
    /// 当前题目提交控制状态（配额 + 冷却）
    /// </summary>
    public ChallengeSubmissionControlStatus? SubmissionControl { get; set; }

    internal static ChallengeDetailModel FromInstance(GameInstance gameInstance, int attemptCount,
        ChallengeInfo? scoreboardChallenge = null, ISet<Guid>? solvedFlagIds = null) =>
        new()
        {
            Id = gameInstance.Challenge.Id,
            Content = gameInstance.Challenge.Content,
            Hints = gameInstance.Challenge.Hints,
            Score = scoreboardChallenge?.Score ?? gameInstance.Challenge.CurrentScore,
            Category = gameInstance.Challenge.Category,
            Title = gameInstance.Challenge.Title,
            Type = gameInstance.Challenge.Type,
            LockDurationMinutes = gameInstance.Challenge.LockDurationMinutes,
            Repository = gameInstance.Challenge.Repository,
            Flags = gameInstance.Challenge.Flags
                .Where(f => f is { IsVisible: true, IsEnabled: true })
                .OrderBy(f => f.SortOrder)
                .Select(f => HybridLineFlagInfoModel.FromChallengeFlag(
                    f, solvedFlagIds?.Contains(f.Id) ?? false))
                .ToList(),
            AllEnabledFlagsSolved = gameInstance.Challenge.Flags.Any(f => f.IsEnabled)
                                    && gameInstance.Challenge.Flags
                                        .Where(f => f.IsEnabled)
                                        .All(f => solvedFlagIds?.Contains(f.Id) ?? false),
            Limit = 0, // SubmissionLimit removed; kept for API backwards compat
            Deadline = gameInstance.Challenge.DeadlineUtc,
            Attempts = attemptCount,
            Context = new()
            {
                InstanceEntry = gameInstance.Container?.Entry,
                CloseTime = gameInstance.Container?.ExpectStopAt,
                Url = gameInstance.AttachmentUrl,
                FileSize = gameInstance.Attachment?.FileSize
            }
        };
}

public class HybridLineFlagInfoModel
{
    public Guid Id { get; set; }
    public int Level { get; set; }
    public Guid TrackId { get; set; }
    public int SortOrder { get; set; }
    public ChallengeFlagAnswerType AnswerType { get; set; }
    public bool IsSolved { get; set; }

    internal static HybridLineFlagInfoModel FromChallengeFlag(ChallengeFlag flag, bool isSolved) =>
        new()
        {
            Id = flag.Id,
            Level = flag.Level,
            TrackId = flag.TrackId,
            SortOrder = flag.SortOrder,
            AnswerType = flag.AnswerType,
            IsSolved = isSolved
        };
}

/// <summary>
/// 选手视图的 Track 状态
/// </summary>
public class TrackStatusModel
{
    public Guid TrackId { get; set; }
    public string? Title { get; set; }
    public bool IsEnabled { get; set; }
    public List<TrackLevelStatusModel> Levels { get; set; } = [];
}

/// <summary>
/// 选手视图的 Track × Level 状态
/// </summary>
public class TrackLevelStatusModel
{
    public int Level { get; set; }
    public string? Title { get; set; }
    /// <summary>Unsolved | Solved | Banned | Locked</summary>
    public string Status { get; set; } = "Unsolved";
    public DateTimeOffset? SolvedAt { get; set; }
    public int AttemptsUsed { get; set; }
    public DateTimeOffset? UnlocksAt { get; set; }
}

/// <summary>
/// 全题锁状态（选手视图）
/// </summary>
public class ChallengeLockStatusModel
{
    public bool IsLocked { get; set; }
    public DateTimeOffset? UnlocksAt { get; set; }
    public string? Reason { get; set; }
    public string? TriggerAnswer { get; set; }
    public Guid? BannedTrackId { get; set; }
    public int? BannedLevel { get; set; }
    public string? BanTriggerAnswer { get; set; }
}
