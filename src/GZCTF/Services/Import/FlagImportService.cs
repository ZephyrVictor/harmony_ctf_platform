using GZCTF.Repositories.Interface;
using GZCTF.Services.Scoring;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace GZCTF.Services.Import;

public class FlagImportService(
    AppDbContext context,
    IChallengeTrackRepository trackRepo,
    IChallengeFlagRepository flagRepo,
    IChallengeLevelConfigRepository levelConfigRepo)
{
    public async Task<FlagImportFilePreview> PreviewFileAsync(
        Stream fileStream, string contentType, CancellationToken token)
    {
        var rows = ParseFile(fileStream, contentType);
        if (rows.Count == 0)
            return new FlagImportFilePreview();

        var columnNames = rows[0].Keys.ToList();
        var sampleRows = rows.Take(5).ToList();

        return new FlagImportFilePreview
        {
            ColumnNames = columnNames,
            SampleRows = sampleRows
        };
    }

    public async Task<FlagImportResult> ImportFlagsAsync(
        int challengeId,
        Stream fileStream,
        string contentType,
        FlagImportColumnMapping mapping,
        ImportMode mode,
        CancellationToken token)
    {
        var rows = ParseFile(fileStream, contentType);
        var result = new FlagImportResult { TotalRows = rows.Count };

        foreach (var (row, idx) in rows.Select((r, i) => (r, i)))
        {
            try
            {
                await using var transaction = await context.Database.BeginTransactionAsync(token);

                var trackMeta = ExtractTrackMeta(row, mapping);
                var matchValue = GetMatchValue(trackMeta, mapping.MatchBy);

                if (string.IsNullOrWhiteSpace(matchValue))
                {
                    result.Errors.Add(new FlagImportError { Row = idx + 1, Error = $"MatchBy column '{mapping.MatchBy}' is empty" });
                    result.Failed++;
                    await transaction.RollbackAsync(token);
                    continue;
                }

                var existingTrack = await FindTrackByMatch(challengeId, mapping.MatchBy, matchValue, token);

                if (existingTrack is not null)
                {
                    if (mode == ImportMode.Skip)
                    {
                        result.Skipped++;
                        await transaction.RollbackAsync(token);
                        continue;
                    }
                    if (mode == ImportMode.Fail)
                    {
                        result.Errors.Add(new FlagImportError { Row = idx + 1, Error = "Track exists and mode=Fail" });
                        result.Failed++;
                        await transaction.RollbackAsync(token);
                        continue;
                    }
                    // Upsert: 若有提交记录则软删旧 track 新建，否则更新
                    var hasSubmissions = await trackRepo.HasSubmissions(existingTrack.Id, token);
                    if (hasSubmissions)
                    {
                        await trackRepo.DeleteTrack(existingTrack.Id, token);
                        existingTrack = null;
                    }
                    else
                    {
                        existingTrack.Title = trackMeta.Title ?? existingTrack.Title;
                        existingTrack.FlawNo = trackMeta.FlawNo ?? existingTrack.FlawNo;
                        existingTrack.Repository = trackMeta.Repository ?? existingTrack.Repository;
                        existingTrack.Module = trackMeta.Module ?? existingTrack.Module;
                        existingTrack.FilePath = trackMeta.FilePath ?? existingTrack.FilePath;
                        existingTrack.Category = trackMeta.Category ?? existingTrack.Category;
                        existingTrack.ReferenceUrl = trackMeta.ReferenceUrl ?? existingTrack.ReferenceUrl;
                        existingTrack = await trackRepo.UpdateTrack(existingTrack, token);

                        // 删除旧 flags（已确认无提交记录）
                        var oldFlags = await flagRepo.GetFlagsByTrack(existingTrack.Id, token);
                        foreach (var f in oldFlags)
                            await flagRepo.DeleteFlag(f.Id, token);
                    }
                    result.Updated++;
                }

                var track = existingTrack ?? new ChallengeTrack
                {
                    Id = Guid.NewGuid(),
                    ChallengeId = challengeId,
                    Title = trackMeta.Title,
                    FlawNo = trackMeta.FlawNo,
                    Repository = trackMeta.Repository,
                    Module = trackMeta.Module,
                    FilePath = trackMeta.FilePath,
                    Category = trackMeta.Category,
                    ReferenceUrl = trackMeta.ReferenceUrl,
                    IsEnabled = true,
                    SortOrder = idx
                };

                if (existingTrack is null)
                {
                    track = await trackRepo.CreateTrack(track, token);
                    result.Imported++;
                }

                // 创建 flags
                foreach (var lc in mapping.LevelColumns)
                {
                    if (!row.TryGetValue(lc.ColumnName, out var answer) || string.IsNullOrWhiteSpace(answer))
                        continue;

                    var flagValue = answer.Trim();

                    // 检查重复 flag value（同挑战内）
                    var dup = await flagRepo.ExistsByValue(challengeId, flagValue, token);
                    if (dup)
                    {
                        result.Errors.Add(new FlagImportError
                        {
                            Row = idx + 1,
                            Error = $"Flag value '{flagValue}' already exists for this challenge"
                        });
                        result.Failed++;
                        await transaction.RollbackAsync(token);
                        goto nextRow;
                    }

                    var flag = new ChallengeFlag
                    {
                        Id = Guid.NewGuid(),
                        ChallengeId = challengeId,
                        FlagValue = flagValue,
                        AnswerType = lc.AnswerType,
                        RawAnswer = flagValue,
                        NormalizedAnswerKey = ScoringService.NormalizeHybridLineAnswer(flagValue, lc.AnswerType),
                        Level = lc.Level,
                        TrackId = track.Id,
                        IsEnabled = true,
                        IsVisible = true,
                        SortOrder = 0
                    };
                    await flagRepo.CreateFlag(flag, token);

                    // Upsert LevelConfig
                    var levelConfig = await levelConfigRepo.GetConfig(challengeId, lc.Level, token);
                    if (levelConfig is null)
                    {
                        await levelConfigRepo.CreateConfig(new ChallengeLevelConfig
                        {
                            ChallengeId = challengeId,
                            Level = lc.Level,
                            Title = lc.LevelTitle,
                            Score = lc.Score,
                            SubmissionLimit = lc.SubmissionLimit,
                            PenaltyScore = lc.PenaltyScore,
                            LockOnAnswers = lc.LockOnAnswers,
                            BanOnAnswers = lc.BanOnAnswers,
                            IsEnabled = true,
                            SortOrder = lc.Level
                        }, token);
                    }
                }

                await transaction.CommitAsync(token);
                continue;

            nextRow:
                continue;
            }
            catch (Exception ex)
            {
                result.Errors.Add(new FlagImportError { Row = idx + 1, Error = ex.Message });
                result.Failed++;
            }
        }

        return result;
    }

    private List<Dictionary<string, string>> ParseFile(Stream stream, string contentType)
    {
        if (contentType.Contains("csv", StringComparison.OrdinalIgnoreCase)
            || contentType.Contains("text/plain", StringComparison.OrdinalIgnoreCase))
            return ParseCsv(stream);

        return ParseXlsx(stream);
    }

    private List<Dictionary<string, string>> ParseXlsx(Stream stream)
    {
        var workbook = new XSSFWorkbook(stream);
        var sheet = workbook.GetSheetAt(0);
        var rows = new List<Dictionary<string, string>>();

        if (sheet.LastRowNum < 0) return rows;

        var headerRow = sheet.GetRow(0);
        var headers = new List<string>();
        for (var c = 0; c < headerRow.LastCellNum; c++)
        {
            var cell = headerRow.GetCell(c);
            headers.Add(cell?.ToString()?.Trim() ?? $"Column{c}");
        }

        for (var r = 1; r <= sheet.LastRowNum; r++)
        {
            var row = sheet.GetRow(r);
            if (row is null) continue;
            var dict = new Dictionary<string, string>();
            for (var c = 0; c < headers.Count; c++)
            {
                var cell = row.GetCell(c);
                dict[headers[c]] = cell?.ToString()?.Trim() ?? string.Empty;
            }
            rows.Add(dict);
        }

        return rows;
    }

    private List<Dictionary<string, string>> ParseCsv(Stream stream)
    {
        var rows = new List<Dictionary<string, string>>();
        using var reader = new StreamReader(stream);
        string? headerLine = reader.ReadLine();
        if (headerLine is null) return rows;

        var headers = headerLine.Split(',').Select(h => h.Trim().Trim('"')).ToList();

        while (reader.ReadLine() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var fields = line.Split(',').Select(f => f.Trim().Trim('"')).ToList();
            var dict = new Dictionary<string, string>();
            for (var i = 0; i < headers.Count && i < fields.Count; i++)
                dict[headers[i]] = fields[i];
            rows.Add(dict);
        }

        return rows;
    }

    private static (
        string? Title, string? FlawNo, string? Repository, string? Module,
        string? FilePath, string? Category, string? ReferenceUrl
    ) ExtractTrackMeta(Dictionary<string, string> row, FlagImportColumnMapping mapping)
    {
        string? Val(string? col) => col is not null && row.TryGetValue(col, out var v) ? v : null;
        return (
            Val(mapping.TrackTitleColumn),
            Val(mapping.TrackFlawNoColumn),
            Val(mapping.TrackRepositoryColumn),
            Val(mapping.TrackModuleColumn),
            Val(mapping.TrackFilePathColumn),
            Val(mapping.TrackCategoryColumn),
            Val(mapping.TrackReferenceUrlColumn)
        );
    }

    private static string? GetMatchValue(
        (string? Title, string? FlawNo, string? Repository, string? Module, string? FilePath, string? Category, string? ReferenceUrl) meta,
        string matchBy) => matchBy switch
        {
            "FlawNo" => meta.FlawNo,
            "Title" => meta.Title,
            _ => meta.FilePath
        };

    private async Task<ChallengeTrack?> FindTrackByMatch(int challengeId, string matchBy, string value, CancellationToken token)
    {
        var tracks = await trackRepo.GetTracks(challengeId, token);
        return matchBy switch
        {
            "FlawNo" => tracks.FirstOrDefault(t => t.FlawNo == value),
            "Title" => tracks.FirstOrDefault(t => t.Title == value),
            _ => tracks.FirstOrDefault(t => t.FilePath == value)
        };
    }
}
