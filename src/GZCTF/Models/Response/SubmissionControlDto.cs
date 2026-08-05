using System.ComponentModel.DataAnnotations;
using GZCTF.Models.Data;

namespace GZCTF.Models.Response;

public class SubmissionControlOverviewDto
{
    public SubmissionControlGameConfigDto Game { get; set; } = new();
    public SubmissionControlChallengeConfigDto[] Challenges { get; set; } = [];
    public SubmissionControlTeamSummaryDto[] Teams { get; set; } = [];
    public DateTimeOffset DailyResetAt { get; set; }
}

public class SubmissionControlGameConfigDto
{
    public int GameId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int DailyTeamSubmissionLimit { get; set; }
    public int CooldownMinutes { get; set; }
    public int ChallengeCooldownWrongAnswerLimitFactor { get; set; }
    public int ChallengeDailyTeamSubmissionLimitFactor { get; set; }
    public int ChallengeTotalTeamSubmissionLimitFactor { get; set; }
}

public class SubmissionControlChallengeConfigDto
{
    public int ChallengeId { get; set; }
    public string Title { get; set; } = string.Empty;
    public ChallengeCategory Category { get; set; }
    public bool IsEnabled { get; set; }
    public int TrackCount { get; set; }
    public int EffectiveWrongAnswerLimit { get; set; }
    public int CooldownWrongAnswerLimitOverride { get; set; }
    public int DailyTeamSubmissionLimit { get; set; }
    public int TotalTeamSubmissionLimit { get; set; }
    public SubmissionControlLimitDto CooldownWrongAnswerLimit { get; set; } = new();
    public SubmissionControlLimitDto DailyTeamSubmission { get; set; } = new();
    public SubmissionControlLimitDto TotalTeamSubmission { get; set; } = new();
}

public class SubmissionControlLimitDto
{
    public SubmissionControlLimitMode Mode { get; set; }
    public int CustomValue { get; set; }
    public int DefaultValue { get; set; }
    public int EffectiveValue { get; set; }
}

public class SubmissionControlTeamSummaryDto
{
    public int ParticipationId { get; set; }
    public int TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public ParticipationStatus ParticipationStatus { get; set; }
    public int TodaySubmissions { get; set; }
    public int TotalSubmissions { get; set; }
    public int AcceptedSubmissions { get; set; }
    public int WrongSubmissions { get; set; }
    public int GameDailyLimit { get; set; }
    public int GameDailyRemaining { get; set; }
    public bool GameDailyExceeded { get; set; }
    public int CoolingChallenges { get; set; }
    public int BlockedChallenges { get; set; }
    public DateTimeOffset? LastSubmissionAt { get; set; }
}

public class SubmissionControlTeamDetailDto : SubmissionControlTeamSummaryDto
{
    public SubmissionControlTeamChallengeStatusDto[] Challenges { get; set; } = [];
}

public class SubmissionControlTeamChallengeStatusDto
{
    public int ChallengeId { get; set; }
    public string Title { get; set; } = string.Empty;
    public ChallengeCategory Category { get; set; }
    public int TodayTeamUsed { get; set; }
    public int TotalTeamUsed { get; set; }
    public int DailyTeamLimit { get; set; }
    public int DailyTeamRemaining { get; set; }
    public bool DailyTeamExceeded { get; set; }
    public int TotalTeamLimit { get; set; }
    public int TotalTeamRemaining { get; set; }
    public bool TotalTeamExceeded { get; set; }
    public int WrongCount { get; set; }
    public int EffectiveWrongAnswerLimit { get; set; }
    public int CooldownWrongAnswerLimitOverride { get; set; }
    public int CooldownMinutes { get; set; }
    public DateTimeOffset? CooldownUntil { get; set; }
    public bool IsCoolingDown { get; set; }
}

public class SubmissionControlUpdateModel
{
    [Range(0, int.MaxValue)]
    public int DailyTeamSubmissionLimit { get; set; }

    [Range(0, int.MaxValue)]
    public int CooldownMinutes { get; set; }

    [Range(0, int.MaxValue)]
    public int ChallengeCooldownWrongAnswerLimitFactor { get; set; } = 5;

    [Range(0, int.MaxValue)]
    public int ChallengeDailyTeamSubmissionLimitFactor { get; set; } = 20;

    [Range(0, int.MaxValue)]
    public int ChallengeTotalTeamSubmissionLimitFactor { get; set; } = 200;

    public bool ApplyDefaultsToAllChallenges { get; set; }

    public SubmissionControlChallengeUpdateModel[] Challenges { get; set; } = [];
}

public class SubmissionControlChallengeUpdateModel
{
    public int ChallengeId { get; set; }

    [Range(0, int.MaxValue)]
    public int DailyTeamSubmissionLimit { get; set; }

    [Range(0, int.MaxValue)]
    public int TotalTeamSubmissionLimit { get; set; }

    [Range(0, int.MaxValue)]
    public int CooldownWrongAnswerLimitOverride { get; set; }

    public SubmissionControlLimitMode? CooldownWrongAnswerLimitMode { get; set; }

    public SubmissionControlLimitMode? DailyTeamSubmissionLimitMode { get; set; }

    public SubmissionControlLimitMode? TotalTeamSubmissionLimitMode { get; set; }
}
