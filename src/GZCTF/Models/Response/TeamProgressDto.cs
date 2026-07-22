namespace GZCTF.Models.Response;

public class TeamProgressSummaryDto
{
    public int ParticipationId { get; set; }
    public int TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public string TeamToken { get; set; } = string.Empty;
    public ParticipationStatus ParticipationStatus { get; set; }
    public string[] Members { get; set; } = [];
    public int TotalSubmissions { get; set; }
    public int AcceptedSubmissions { get; set; }
    public int WrongSubmissions { get; set; }
    public int PenaltySubmissions { get; set; }
    public int SolvedLevels { get; set; }
    public int TotalLevels { get; set; }
    public int CompletedTracks { get; set; }
    public int TotalTracks { get; set; }
    public int ZeroDayApprovedCount { get; set; }
    public int ZeroDayScore { get; set; }
    public DateTimeOffset? LastSubmissionAt { get; set; }
}

public class TeamProgressDetailDto : TeamProgressSummaryDto
{
    public ChallengeProgressDto[] Challenges { get; set; } = [];
    public TeamSubmissionDto[] Submissions { get; set; } = [];
    public ZeroDayBonusBriefDto[] ZeroDayBonuses { get; set; } = [];
}

public class ChallengeProgressDto
{
    public int ChallengeId { get; set; }
    public string ChallengeTitle { get; set; } = string.Empty;
    public ChallengeCategory Category { get; set; }
    public TrackProgressDto[] Tracks { get; set; } = [];
}

public class TrackProgressDto
{
    public Guid TrackId { get; set; }
    public string? Title { get; set; }
    public string? Repository { get; set; }
    public string? Module { get; set; }
    public string? FilePath { get; set; }
    public string? Category { get; set; }
    public bool IsEnabled { get; set; }
    public LevelProgressDto[] Levels { get; set; } = [];
}

public class LevelProgressDto
{
    public int Level { get; set; }
    public string? Title { get; set; }
    public int Score { get; set; }
    public string Status { get; set; } = "Unsolved";
    public DateTimeOffset? SolvedAt { get; set; }
    public int? AcceptedSubmissionId { get; set; }
    public string? AcceptedAnswer { get; set; }
    public int Attempts { get; set; }
    public int WrongAttempts { get; set; }
    public DateTimeOffset? LatestSubmissionAt { get; set; }
}

public class TeamSubmissionDto
{
    public int SubmissionId { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int ChallengeId { get; set; }
    public string ChallengeTitle { get; set; } = string.Empty;
    public Guid? TrackId { get; set; }
    public int? Level { get; set; }
    public string Answer { get; set; } = string.Empty;
    public AnswerResult Status { get; set; }
    public string? SubmissionType { get; set; }
    public int ScoreChange { get; set; }
    public bool IsPenalty { get; set; }
}

public class ZeroDayBonusBriefDto
{
    public Guid Id { get; set; }
    public ZeroDaySeverity? Severity { get; set; }
    public int Delta { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? RelatedInfo { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
