using GZCTF.Models.Data;
using GZCTF.Services.Scoring;
using GZCTF.Utils;
using Xunit;

namespace GZCTF.Test.UnitTests.Scoring;

/// <summary>
/// Tests for ScoringService pure helper functions (NormalizeHybridLineAnswer, TryParseLineRange).
/// The full ProcessSubmission flow requires database state and is covered by integration tests.
/// </summary>
public class ScoringServiceTests
{
    [Fact]
    public void CalculateWrongPenalty_LevelZeroPenalty_DoesNotFallbackToDefaultPenalty()
    {
        var config = new ChallengeLevelConfig { PenaltyScore = 0 };

        var penalty = ScoringService.CalculateWrongPenaltyForTest(config, challengePenaltyScore: 2, defaultPenaltyScore: 2);

        Assert.Equal(0, penalty);
    }

    [Fact]
    public void CalculateWrongPenalty_NoLevelConfig_UsesChallengeOrDefaultPenalty()
    {
        Assert.Equal(3, ScoringService.CalculateWrongPenaltyForTest(null, challengePenaltyScore: 3, defaultPenaltyScore: 2));
        Assert.Equal(2, ScoringService.CalculateWrongPenaltyForTest(null, challengePenaltyScore: null, defaultPenaltyScore: 2));
    }

    [Fact]
    public void NormalizeHybridLineAnswer_Exact_NumericReturnsNormalized()
    {
        // Exact answer type falls through to InferHybridLineAnswer which only parses line numbers/ranges.
        // For non-numeric input, normalization returns empty (caller falls back to literal FlagValue comparison).
        var normalized = ScoringService.NormalizeHybridLineAnswer("abc123", ChallengeFlagAnswerType.Exact);
        Assert.Equal(string.Empty, normalized);
    }

    [Fact]
    public void NormalizeHybridLineAnswer_Exact_NumericSingleLineReturnsNormalized()
    {
        var normalized = ScoringService.NormalizeHybridLineAnswer("56", ChallengeFlagAnswerType.Exact);
        Assert.StartsWith("S:", normalized);
        Assert.Contains("56", normalized);
    }

    [Fact]
    public void NormalizeHybridLineAnswer_SingleLine_NormalizesCorrectly()
    {
        var normalized = ScoringService.NormalizeHybridLineAnswer("c:56", ChallengeFlagAnswerType.SingleLine);
        Assert.Contains("56", normalized);
        Assert.StartsWith("S:", normalized);
    }

    [Fact]
    public void NormalizeHybridLineAnswer_LineRange_NormalizesCorrectly()
    {
        var normalized = ScoringService.NormalizeHybridLineAnswer("c:77-82", ChallengeFlagAnswerType.LineRange);
        Assert.StartsWith("R:", normalized);
        Assert.Contains("77", normalized);
        Assert.Contains("82", normalized);
    }

    [Fact]
    public void NormalizeHybridLineAnswer_MultiLine_ParsesCommaSeparated()
    {
        var normalized = ScoringService.NormalizeHybridLineAnswer("c:110,c:121", ChallengeFlagAnswerType.MultiLine);
        Assert.StartsWith("M:", normalized);
        Assert.Contains("110", normalized);
        Assert.Contains("121", normalized);
    }

    [Fact]
    public void NormalizeHybridLineAnswer_EmptyInput_ReturnsEmpty()
    {
        var normalized = ScoringService.NormalizeHybridLineAnswer("", ChallengeFlagAnswerType.Exact);
        Assert.Equal(string.Empty, normalized);
    }

    [Fact]
    public void NormalizeHybridLineAnswer_InvalidLineRange_ReturnsEmpty()
    {
        var normalized = ScoringService.NormalizeHybridLineAnswer("abc", ChallengeFlagAnswerType.LineRange);
        Assert.Equal(string.Empty, normalized);
    }

    [Fact]
    public void TryNormalizeHybridLineAnswer_ValidSingleLine_ReturnsTrue()
    {
        var ok = ScoringService.TryNormalizeHybridLineAnswer("c:56", ChallengeFlagAnswerType.SingleLine, out var normalized);
        Assert.True(ok);
        Assert.StartsWith("S:", normalized);
    }

    [Fact]
    public void TryNormalizeHybridLineAnswer_Invalid_ReturnsFalse()
    {
        var ok = ScoringService.TryNormalizeHybridLineAnswer("not-a-number", ChallengeFlagAnswerType.SingleLine, out var normalized);
        Assert.False(ok);
        Assert.Equal(string.Empty, normalized);
    }

    [Theory]
    [InlineData("ecmascript/base/json_stringifier_optimized.cpp:857")]
    [InlineData("ecmascript/base/json_stringifier_optimized.cpp:856-863")]
    [InlineData("ecmascript/base/json_stringifier_optimized.cpp:c:856-864")]
    public void SourceLocation_Range_AcceptsLineOrSubRange(string submitted)
    {
        var flag = new ChallengeFlag
        {
            FlagValue = "ecmascript/base/json_stringifier_optimized.cpp:856-864",
            RawAnswer = "ecmascript/base/json_stringifier_optimized.cpp:856-864",
            AnswerType = ChallengeFlagAnswerType.SourceLocation
        };
        var config = new ChallengeLevelConfig { OrSeparators = "、,;", RangeSeparator = "-", StripLinePrefix = true };

        Assert.True(ScoringService.TryMatchForTest(flag, submitted, config));
    }

    [Theory]
    [InlineData("ecmascript/base/json_stringifier_optimized.cpp:855")]
    [InlineData("ecmascript/base/json_stringifier_optimized.cpp:856-865")]
    [InlineData("ecmascript/base/other.cpp:857")]
    public void SourceLocation_Range_RejectsOutsideOrWrongPath(string submitted)
    {
        var flag = new ChallengeFlag
        {
            FlagValue = "ecmascript/base/json_stringifier_optimized.cpp:856-864",
            RawAnswer = "ecmascript/base/json_stringifier_optimized.cpp:856-864",
            AnswerType = ChallengeFlagAnswerType.SourceLocation
        };
        var config = new ChallengeLevelConfig { OrSeparators = "、,;", RangeSeparator = "-", StripLinePrefix = true };

        Assert.False(ScoringService.TryMatchForTest(flag, submitted, config));
    }

    [Theory]
    [InlineData("ecmascript/builtins/builtins.cpp:281")]
    [InlineData("ecmascript/builtins/builtins.cpp:286")]
    [InlineData("ecmascript/builtins/builtins.cpp:c:286")]
    public void SourceLocation_OrLines_AcceptsAnyListedLine(string submitted)
    {
        var flag = new ChallengeFlag
        {
            FlagValue = "ecmascript/builtins/builtins.cpp:281、286",
            RawAnswer = "ecmascript/builtins/builtins.cpp:281、286",
            AnswerType = ChallengeFlagAnswerType.SourceLocation
        };
        var config = new ChallengeLevelConfig { OrSeparators = "、,;", RangeSeparator = "-", StripLinePrefix = true };

        Assert.True(ScoringService.TryMatchForTest(flag, submitted, config));
    }

    [Theory]
    [InlineData("ecmascript/builtins/builtins.cpp:286")]
    [InlineData("ecmascript/base/json_stringifier_optimized.cpp:857")]
    [InlineData("ecmascript/base/json_stringifier_optimized.cpp:856-864")]
    public void SourceLocation_OrFullLocations_AcceptsAnyListedLocation(string submitted)
    {
        var flag = new ChallengeFlag
        {
            FlagValue = "ecmascript/builtins/builtins.cpp:286、ecmascript/base/json_stringifier_optimized.cpp:856-864",
            RawAnswer = "ecmascript/builtins/builtins.cpp:286、ecmascript/base/json_stringifier_optimized.cpp:856-864",
            AnswerType = ChallengeFlagAnswerType.SourceLocation
        };
        var config = new ChallengeLevelConfig { OrSeparators = "、,;", RangeSeparator = "-", StripLinePrefix = true };

        Assert.True(ScoringService.TryMatchForTest(flag, submitted, config));
    }

    [Fact]
    public void SourceLocation_CaseInsensitivePath_AcceptsDifferentPathCase()
    {
        var flag = new ChallengeFlag
        {
            FlagValue = "ecmascript/base/json_stringifier_optimized.cpp:856-864",
            RawAnswer = "ecmascript/base/json_stringifier_optimized.cpp:856-864",
            AnswerType = ChallengeFlagAnswerType.SourceLocation
        };
        var config = new ChallengeLevelConfig
        {
            CaseSensitive = false,
            PathCaseSensitive = false,
            OrSeparators = "、,;",
            RangeSeparator = "-",
            StripLinePrefix = true
        };

        Assert.True(ScoringService.TryMatchForTest(flag, "ECMASCRIPT/BASE/JSON_STRINGIFIER_OPTIMIZED.CPP:857", config));
    }

    [Fact]
    public void TryParseLineRange_ValidSingleLine_ReturnsTrue()
    {
        var ok = ScoringService.TryParseLineRange("56", out var start, out var end);
        Assert.True(ok);
        Assert.Equal(56, start);
        Assert.Equal(56, end);
    }

    [Fact]
    public void TryParseLineRange_ValidRange_ReturnsTrue()
    {
        var ok = ScoringService.TryParseLineRange("77-82", out var start, out var end);
        Assert.True(ok);
        Assert.Equal(77, start);
        Assert.Equal(82, end);
    }

    [Fact]
    public void TryParseLineRange_CPrefix_ValidSingleLine()
    {
        var ok = ScoringService.TryParseLineRange("c:56", out var start, out _);
        Assert.True(ok);
        Assert.Equal(56, start);
    }

    [Fact]
    public void TryParseLineRange_CPrefix_ValidRange()
    {
        var ok = ScoringService.TryParseLineRange("c:77-82", out var start, out var end);
        Assert.True(ok);
        Assert.Equal(77, start);
        Assert.Equal(82, end);
    }

    [Fact]
    public void TryParseLineRange_Invalid_ReturnsFalse()
    {
        var ok = ScoringService.TryParseLineRange("abc", out _, out _);
        Assert.False(ok);
    }

    [Fact]
    public void TryParseLineRange_ReversedRange_ReturnsFalse()
    {
        var ok = ScoringService.TryParseLineRange("82-77", out _, out _);
        Assert.False(ok);
    }

    [Fact]
    public void TryParseLineRange_ZeroLine_ReturnsFalse()
    {
        var ok = ScoringService.TryParseLineRange("0", out _, out _);
        Assert.False(ok);
    }

    [Fact]
    public void TryParseLineRange_Empty_ReturnsFalse()
    {
        var ok = ScoringService.TryParseLineRange("", out _, out _);
        Assert.False(ok);
    }

    [Fact]
    public void TryParseLineRange_Whitespace_ReturnsFalse()
    {
        var ok = ScoringService.TryParseLineRange("   ", out _, out _);
        Assert.False(ok);
    }

    [Fact]
    public void ScoreResult_DefaultValues_AreInitialized()
    {
        var result = new ScoreResult();
        Assert.Equal(0, result.ScoreChange);
        Assert.False(result.IsPenalty);
        Assert.False(result.ChallengeLocked);
        Assert.False(result.TrackCompleted);
        Assert.False(result.ChallengeFirstSolve);
        Assert.Null(result.Level);
        Assert.Null(result.TrackId);
        Assert.Null(result.MatchedFlagId);
        Assert.Null(result.BannedTrackId);
        Assert.Null(result.BannedLevel);
        Assert.Null(result.UnlocksAt);
        Assert.Null(result.Message);
    }
}
