using System;
using System.Collections.Generic;
using GZCTF.Services.Import;
using GZCTF.Services.Scoring;
using GZCTF.Utils;
using Xunit;

namespace GZCTF.Test.UnitTests.Import;

/// <summary>
/// Tests for FlagImport model defaults and ImportMode enum.
/// Full FlagImportService tests (file parsing + upsert) require a database fixture
/// and are covered by integration tests.
/// </summary>
public class FlagImportModelsTests
{
    [Fact]
    public void FlagImportColumnMapping_DefaultsToFilePathMatch()
    {
        var mapping = new FlagImportColumnMapping();
        Assert.Equal("L1", mapping.MatchBy);
        Assert.Empty(mapping.LevelColumns);
        Assert.Null(mapping.TrackTitleColumn);
        Assert.Null(mapping.TrackFilePathColumn);
    }

    [Fact]
    public void LevelColumnMapping_DefaultsToExactAnswerType()
    {
        var lc = new LevelColumnMapping();
        Assert.Equal(ChallengeFlagAnswerType.Exact, lc.AnswerType);
        Assert.Empty(lc.LockOnAnswers);
        Assert.Empty(lc.BanOnAnswers);
        Assert.Equal(string.Empty, lc.ColumnName);
        Assert.True(lc.CaseSensitive);
        Assert.True(lc.PathCaseSensitive);
        Assert.True(lc.StripLinePrefix);
        Assert.True(lc.NormalizeSourceLocation);
        Assert.Equal("、,;", lc.OrSeparators);
        Assert.Equal("-", lc.RangeSeparator);
    }

    [Fact]
    public void LevelColumnMapping_Level2CanCarryDefaultCommitTrapAnswers()
    {
        var lc = new LevelColumnMapping
        {
            Level = 2,
            BanOnAnswers = ["0", "hash(0)", "0000000000000000000000000000000000000000"]
        };

        Assert.Contains("hash(0)", lc.BanOnAnswers);
        Assert.Contains("0", lc.BanOnAnswers);
        Assert.Contains("0000000000000000000000000000000000000000", lc.BanOnAnswers);
    }

    [Fact]
    public void CalculateImportLevelScoreSum_DefaultDatasetScores_ReturnsTen()
    {
        var mapping = new FlagImportColumnMapping
        {
            LevelColumns =
            [
                new() { Level = 1, Score = 5 },
                new() { Level = 2, Score = 4 },
                new() { Level = 3, Score = 1 }
            ]
        };

        Assert.Equal(10, ChallengeScoreService.CalculateImportLevelScoreSum(mapping.LevelColumns));
    }

    [Fact]
    public void CalculateImportLevelScoreSum_CustomDatasetScores_ReturnsTwentyOne()
    {
        var mapping = new FlagImportColumnMapping
        {
            LevelColumns =
            [
                new() { Level = 1, Score = 8 },
                new() { Level = 2, Score = 7 },
                new() { Level = 3, Score = 6 }
            ]
        };

        Assert.Equal(21, ChallengeScoreService.CalculateImportLevelScoreSum(mapping.LevelColumns));
    }

    [Fact]
    public void BuildFlagValue_NormalizesSourceLocationForScorer()
    {
        var row = new Dictionary<string, string>
        {
            ["文件路径"] = "arkweb/ohos_nweb/src/cef_delegate/nweb_handler_delegate.cc",
            ["代码行数"] = "c:4452"
        };
        var lc = new LevelColumnMapping
        {
            Level = 1,
            ColumnName = "文件路径",
            ExtraColumns = ["代码行数"],
            AnswerType = ChallengeFlagAnswerType.SourceLocation
        };

        var rawFlag = FlagImportService.BuildRawFlagValueForTest(row, lc);
        var flag = FlagImportService.BuildFlagValueForTest(row, lc);

        Assert.Equal("arkweb/ohos_nweb/src/cef_delegate/nweb_handler_delegate.cc:c:4452", rawFlag);
        Assert.Equal("arkweb/ohos_nweb/src/cef_delegate/nweb_handler_delegate.cc:4452", flag);
        Assert.Null(FlagImportService.ValidateImportedFlagValueForTest(flag!, lc));
    }

    [Fact]
    public void ValidateRowFlagValues_RejectsMalformedRepeatedColon()
    {
        var row = new Dictionary<string, string>
        {
            ["文件路径"] = "ecmascript/builtins/builtins_regexp.cpp",
            ["代码行数"] = "c:44、c::47"
        };
        var lc = new LevelColumnMapping
        {
            Level = 1,
            ColumnName = "文件路径",
            ExtraColumns = ["代码行数"],
            AnswerType = ChallengeFlagAnswerType.SourceLocation
        };

        var rawFlag = FlagImportService.BuildRawFlagValueForTest(row, lc);
        var error = FlagImportService.ValidateImportedFlagValueForTest(rawFlag!, lc);

        Assert.Equal("ecmascript/builtins/builtins_regexp.cpp:c:44、c::47", rawFlag);
        Assert.Contains("malformed '::'", error);
    }

    [Fact]
    public void ValidateImportedFlagValue_RejectsUnparseableSourceLocation()
    {
        var lc = new LevelColumnMapping
        {
            Level = 1,
            ColumnName = "L1",
            AnswerType = ChallengeFlagAnswerType.SourceLocation
        };

        var error = FlagImportService.ValidateImportedFlagValueForTest(
            "arkweb/ohos_nweb/src/cef_delegate/nweb_handler_delegate.cc:c4452", lc);

        Assert.Contains("SourceLocation cannot be parsed", error);
    }

    [Fact]
    public void ValidateImportedFlagValue_RejectsUnverifiablePlaceholder()
    {
        var lc = new LevelColumnMapping
        {
            Level = 3,
            ColumnName = "CWE",
            AnswerType = ChallengeFlagAnswerType.Exact
        };

        var error = FlagImportService.ValidateImportedFlagValueForTest("无法验证", lc);

        Assert.Contains("placeholder value", error);
    }

    [Fact]
    public void ValidateRowFlagValues_RejectsRowsWithAnyInvalidLevel()
    {
        var row = new Dictionary<string, string>
        {
            ["文件路径"] = "ecmascript/js_api/js_api_lightweightset.cpp",
            ["代码行数"] = "c:114",
            ["commit id"] = "65e0172fae69b7477bdd060bf70938c2264a7020",
            ["CWE(修复研判)"] = "无法验证"
        };
        var mapping = new FlagImportColumnMapping
        {
            LevelColumns =
            [
                new()
                {
                    Level = 1,
                    ColumnName = "文件路径",
                    ExtraColumns = ["代码行数"],
                    AnswerType = ChallengeFlagAnswerType.SourceLocation
                },
                new()
                {
                    Level = 2,
                    ColumnName = "commit id",
                    AnswerType = ChallengeFlagAnswerType.Exact
                },
                new()
                {
                    Level = 3,
                    ColumnName = "CWE(修复研判)",
                    AnswerType = ChallengeFlagAnswerType.Exact
                }
            ]
        };

        var error = FlagImportService.ValidateRowFlagValuesForTest(row, mapping);

        Assert.Contains("Invalid Level 3", error);
        Assert.Contains("无法验证", error);
    }

    [Fact]
    public void FlagImportFilePreview_DefaultsAreEmpty()
    {
        var preview = new FlagImportFilePreview();
        Assert.Empty(preview.ColumnNames);
        Assert.Empty(preview.SampleRows);
    }

    [Fact]
    public void FlagImportResult_DefaultsAreZero()
    {
        var result = new FlagImportResult();
        Assert.Equal(0, result.TotalRows);
        Assert.Equal(0, result.Imported);
        Assert.Equal(0, result.Updated);
        Assert.Equal(0, result.Skipped);
        Assert.Equal(0, result.Failed);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void FlagImportError_DefaultsAreEmpty()
    {
        var err = new FlagImportError();
        Assert.Equal(0, err.Row);
        Assert.Equal(string.Empty, err.Error);
    }

    [Theory]
    [InlineData(ImportMode.Upsert)]
    [InlineData(ImportMode.Skip)]
    [InlineData(ImportMode.Fail)]
    public void ImportMode_AllValuesDefined(ImportMode mode)
    {
        Assert.True(Enum.IsDefined(mode));
    }

    [Fact]
    public void ImportMode_HasThreeValues()
    {
        var values = Enum.GetValues<ImportMode>();
        Assert.Equal(3, values.Length);
    }
}
