using System;
using GZCTF.Models.Data;
using GZCTF.Models.Request.Game;
using GZCTF.Repositories;
using GZCTF.Services.Scoring;
using Xunit;

namespace GZCTF.Test.UnitTests.Scoring;

public class SubmissionQuotaServiceTests
{
    [Fact]
    public void GetPrcDayWindow_UsesPrcMidnightBoundaries()
    {
        var utcNow = new DateTimeOffset(2026, 8, 4, 15, 30, 0, TimeSpan.Zero);

        var window = SubmissionQuotaService.GetPrcDayWindow(utcNow);

        Assert.Equal(new DateOnly(2026, 8, 4), window.Day);
        Assert.Equal(new DateTimeOffset(2026, 8, 3, 16, 0, 0, TimeSpan.Zero), window.StartUtc);
        Assert.Equal(new DateTimeOffset(2026, 8, 4, 16, 0, 0, TimeSpan.Zero), window.EndUtc);
        Assert.Equal(window.EndUtc, window.ResetsAtUtc);
    }

    [Fact]
    public void AdvisoryLockKey_GameDailyTeam_IgnoresChallengeId()
    {
        var day = new DateOnly(2026, 8, 4);

        var first = SubmissionQuotaRepository.AdvisoryLockKey(
            SubmissionControlScope.GameDailyTeam, 1, 10, 100, day);
        var second = SubmissionQuotaRepository.AdvisoryLockKey(
            SubmissionControlScope.GameDailyTeam, 1, 11, 100, day);

        Assert.Equal(first, second);
    }

    [Fact]
    public void AdvisoryLockKey_ChallengeDailyTeam_IncludesParticipationId()
    {
        var day = new DateOnly(2026, 8, 4);

        var first = SubmissionQuotaRepository.AdvisoryLockKey(
            SubmissionControlScope.ChallengeDailyTeam, 1, 10, 100, day);
        var second = SubmissionQuotaRepository.AdvisoryLockKey(
            SubmissionControlScope.ChallengeDailyTeam, 1, 10, 101, day);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void AdvisoryLockKey_ChallengeTeam_IncludesParticipationId()
    {
        var first = SubmissionQuotaRepository.AdvisoryLockKey(
            SubmissionControlScope.ChallengeTotalTeam, 1, 10, 100, null);
        var second = SubmissionQuotaRepository.AdvisoryLockKey(
            SubmissionControlScope.ChallengeTotalTeam, 1, 10, 101, null);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void GetEffectiveWrongAnswerLimit_DefaultsToTrackCountTimesFive()
    {
        var challenge = new GameChallenge
        {
            CooldownWrongAnswerLimitOverride = 0,
            CooldownWrongAnswerLimitMode = SubmissionControlLimitMode.InheritDefault
        };

        var limit = ScoringService.GetEffectiveWrongAnswerLimit(challenge, 3);

        Assert.Equal(15, limit);
    }

    [Fact]
    public void GetEffectiveWrongAnswerLimit_UsesChallengeOverride()
    {
        var challenge = new GameChallenge
        {
            CooldownWrongAnswerLimitOverride = 7,
            CooldownWrongAnswerLimitMode = SubmissionControlLimitMode.Custom
        };

        var limit = ScoringService.GetEffectiveWrongAnswerLimit(challenge, 3);

        Assert.Equal(7, limit);
    }

    [Fact]
    public void EffectiveLimit_InheritDefault_UsesTrackCountTimesFactor()
    {
        var limit = SubmissionControlEffectiveLimits.Resolve(
            SubmissionControlLimitMode.InheritDefault, 99, 4, 20);

        Assert.Equal(80, limit);
    }

    [Fact]
    public void EffectiveLimit_InheritDefault_ZeroFactorIsUnlimited()
    {
        var limit = SubmissionControlEffectiveLimits.Resolve(
            SubmissionControlLimitMode.InheritDefault, 99, 4, 0);

        Assert.Equal(0, limit);
    }

    [Fact]
    public void EffectiveLimit_InheritDefault_ZeroTracksIsUnlimited()
    {
        var limit = SubmissionControlEffectiveLimits.Resolve(
            SubmissionControlLimitMode.InheritDefault, 99, 0, 20);

        Assert.Equal(0, limit);
    }

    [Fact]
    public void EffectiveLimit_Custom_UsesCustomValue()
    {
        var limit = SubmissionControlEffectiveLimits.Resolve(
            SubmissionControlLimitMode.Custom, 33, 4, 20);

        Assert.Equal(33, limit);
    }

    [Fact]
    public void EffectiveLimit_Unlimited_ReturnsZero()
    {
        var limit = SubmissionControlEffectiveLimits.Resolve(
            SubmissionControlLimitMode.Unlimited, 33, 4, 20);

        Assert.Equal(0, limit);
    }
}
