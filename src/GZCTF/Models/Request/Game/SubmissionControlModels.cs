using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;

namespace GZCTF.Models.Request.Game;

[JsonConverter(typeof(JsonStringEnumConverter<SubmissionControlScope>))]
public enum SubmissionControlScope
{
    GameDailyTeam,
    ChallengeTotalTeam,
    ChallengeDailyTeam,
    Cooldown
}

public class SubmissionQuotaStatus
{
    public int Limit { get; set; }
    public int Used { get; set; }
    public int Remaining { get; set; }
    public bool Unlimited { get; set; }
    public bool Exceeded { get; set; }
    public DateTimeOffset? ResetsAt { get; set; }
}

public class ChallengeSubmissionControlStatus
{
    public SubmissionQuotaStatus? GameDailyTeam { get; set; }
    public SubmissionQuotaStatus? ChallengeDailyTeam { get; set; }
    public SubmissionQuotaStatus? ChallengeTotalTeam { get; set; }
    public CooldownInfo? Cooldown { get; set; }
}

public record SubmissionQuotaBlock(
    SubmissionControlScope Scope,
    int Limit,
    int Used,
    int Remaining,
    DateTimeOffset? ResetsAt)
{
    public string Message => Scope switch
    {
        SubmissionControlScope.GameDailyTeam => "今日比赛提交次数已达上限",
        SubmissionControlScope.ChallengeTotalTeam => "本题提交次数已达上限",
        SubmissionControlScope.ChallengeDailyTeam => "今日本题提交次数已达上限",
        SubmissionControlScope.Cooldown => "题目冷却中",
        _ => "提交次数已达上限"
    };
}

public record SubmissionBlockedResponse : RequestResponse
{
    public SubmissionBlockedResponse(string title, SubmissionControlScope scope, int limit, int used, int remaining,
        DateTimeOffset? resetsAt, int status = StatusCodes.Status400BadRequest) : base(title, status)
    {
        Scope = scope;
        Limit = limit;
        Used = used;
        Remaining = remaining;
        ResetsAt = resetsAt;
    }

    public SubmissionControlScope Scope { get; init; }
    public int Limit { get; init; }
    public int Used { get; init; }
    public int Remaining { get; init; }
    public DateTimeOffset? ResetsAt { get; init; }
}
