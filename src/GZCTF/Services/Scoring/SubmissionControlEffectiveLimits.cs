using GZCTF.Models.Data;

namespace GZCTF.Services.Scoring;

public static class SubmissionControlEffectiveLimits
{
    public static int InheritedLimit(int enabledTrackCount, int factor)
    {
        if (enabledTrackCount <= 0 || factor <= 0)
            return 0;

        var value = (long)enabledTrackCount * factor;
        return value > int.MaxValue ? int.MaxValue : (int)value;
    }

    public static int Resolve(
        SubmissionControlLimitMode mode,
        int customValue,
        int enabledTrackCount,
        int factor) =>
        mode switch
        {
            SubmissionControlLimitMode.Custom => Math.Max(0, customValue),
            SubmissionControlLimitMode.Unlimited => 0,
            _ => InheritedLimit(enabledTrackCount, factor)
        };

    public static int CooldownWrongAnswerLimit(Game game, GameChallenge challenge, int enabledTrackCount) =>
        Resolve(
            challenge.CooldownWrongAnswerLimitMode,
            challenge.CooldownWrongAnswerLimitOverride,
            enabledTrackCount,
            game.ChallengeCooldownWrongAnswerLimitFactor);

    public static int ChallengeDailyTeamSubmissionLimit(Game game, GameChallenge challenge, int enabledTrackCount) =>
        Resolve(
            challenge.DailyTeamSubmissionLimitMode,
            challenge.DailyTeamSubmissionLimit,
            enabledTrackCount,
            game.ChallengeDailyTeamSubmissionLimitFactor);

    public static int ChallengeTotalTeamSubmissionLimit(Game game, GameChallenge challenge, int enabledTrackCount) =>
        Resolve(
            challenge.TotalTeamSubmissionLimitMode,
            challenge.TotalTeamSubmissionLimit,
            enabledTrackCount,
            game.ChallengeTotalTeamSubmissionLimitFactor);

    public static SubmissionControlChallengeLimits ForChallenge(
        Game game,
        GameChallenge challenge,
        int enabledTrackCount) =>
        new(
            CooldownWrongAnswerLimit(game, challenge, enabledTrackCount),
            ChallengeDailyTeamSubmissionLimit(game, challenge, enabledTrackCount),
            ChallengeTotalTeamSubmissionLimit(game, challenge, enabledTrackCount));
}

public record SubmissionControlChallengeLimits(
    int CooldownWrongAnswerLimit,
    int DailyTeamSubmissionLimit,
    int TotalTeamSubmissionLimit);
