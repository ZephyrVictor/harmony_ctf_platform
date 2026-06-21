using System;
using GZCTF.Services.Import;
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
        Assert.Equal("FilePath", mapping.MatchBy);
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
