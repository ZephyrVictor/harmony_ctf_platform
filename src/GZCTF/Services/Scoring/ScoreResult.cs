namespace GZCTF.Services.Scoring;

public class ScoreResult
{
    public AnswerResult Status { get; set; }
    public int ScoreChange { get; set; }
    public bool IsPenalty { get; set; }
    public Guid? MatchedFlagId { get; set; }
    public SubmissionType SubmissionType { get; set; }
    public string? Message { get; set; }

    // 多级 flag 新增字段
    public int? Level { get; set; }
    public Guid? TrackId { get; set; }
    public Guid? BannedTrackId { get; set; }
    public int? BannedLevel { get; set; }
    public bool ChallengeLocked { get; set; }
    public DateTimeOffset? UnlocksAt { get; set; }
    public bool TrackCompleted { get; set; }
    public bool ChallengeFirstSolve { get; set; }

    /// <summary>
    /// 题目冷却信息（per-challenge 答错次数 + 临时冷却）
    /// </summary>
    public GZCTF.Models.Request.Game.CooldownInfo? Cooldown { get; set; }
}
