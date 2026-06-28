using GZCTF.Repositories.Interface;
using GZCTF.Services.Scoring;
using Microsoft.EntityFrameworkCore;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace GZCTF.Services.Import;

public class FlagImportService(
    AppDbContext context,
    IChallengeTrackRepository trackRepo,
    IChallengeFlagRepository flagRepo,
    IChallengeLevelConfigRepository levelConfigRepo,
    IGameChallengeRepository challengeRepo,
    IGameRepository gameRepo)
{
    public async Task<FlagImportFilePreview> PreviewFileAsync(
        Stream fileStream, string contentType, FlagImportColumnMapping? mapping, CancellationToken token)
    {
        var rows = ParseFile(fileStream, contentType);
        if (rows.Count == 0)
            return new FlagImportFilePreview();

        var columnNames = rows[0].Keys.ToList();
        var sampleRows = rows.Take(5).ToList();

        var preview = new FlagImportFilePreview
        {
            ColumnNames = columnNames,
            SampleRows = sampleRows
        };

        if (mapping is not null)
        {
            foreach (var row in rows)
            {
                var trackMeta = ExtractTrackMeta(row, mapping);
                var submodule = mapping.SubmoduleColumn is not null && row.TryGetValue(mapping.SubmoduleColumn, out var sm)
                    ? sm : null;
                var repository = mapping.RepositoryColumn is not null && row.TryGetValue(mapping.RepositoryColumn, out var rp)
                    ? rp : trackMeta.Repository;

                var trackPreview = new FlagImportTrackPreview
                {
                    Title = trackMeta.Title,
                    FlawNo = trackMeta.FlawNo,
                    Repository = repository,
                    Module = trackMeta.Module,
                    FilePath = trackMeta.FilePath,
                    Category = trackMeta.Category,
                    ReferenceUrl = trackMeta.ReferenceUrl,
                    Submodule = submodule,
                    Levels = []
                };

                foreach (var lc in mapping.LevelColumns)
                {
                    if (!row.TryGetValue(lc.ColumnName, out var answer) || string.IsNullOrWhiteSpace(answer))
                        continue;
                    var combinedValue = answer.Trim();
                    if (lc.ExtraColumns is { Count: > 0 })
                    {
                        foreach (var extra in lc.ExtraColumns)
                        {
                            if (!row.TryGetValue(extra, out var extraVal) || string.IsNullOrWhiteSpace(extraVal))
                                continue;
                            combinedValue = $"{combinedValue}:{extraVal.Trim()}";
                        }
                    }
                    trackPreview.Levels.Add(new FlagImportLevelPreview
                    {
                        Level = lc.Level,
                        FlagValue = combinedValue,
                        AnswerType = lc.AnswerType.ToString(),
                        Score = lc.Score
                    });
                }

                preview.Tracks.Add(trackPreview);

                if (!string.IsNullOrWhiteSpace(repository) && !preview.Repositories.Contains(repository))
                    preview.Repositories.Add(repository!);
                if (!string.IsNullOrWhiteSpace(submodule) && !preview.Submodules.Contains(submodule))
                    preview.Submodules.Add(submodule!);
            }
        }

        return await Task.FromResult(preview);
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
                // L1 MatchBy: 用 L1 flag 值（含 ExtraColumns 拼接）作为唯一键
                var matchValue = mapping.MatchBy == "L1"
                    ? ComputeL1FlagValue(row, mapping)
                    : GetMatchValue(trackMeta, mapping.MatchBy);

                if (string.IsNullOrWhiteSpace(matchValue))
                {
                    result.Errors.Add(new FlagImportError { Row = idx + 1, Error = $"MatchBy column '{mapping.MatchBy}' is empty" });
                    result.Failed++;
                    await transaction.RollbackAsync(token);
                    continue;
                }

                var existingTrack = mapping.MatchBy == "L1"
                    ? await FindTrackByL1Flag(challengeId, matchValue, token)
                    : await FindTrackByMatch(challengeId, mapping.MatchBy, matchValue, token);

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
                    // 拼接 ExtraColumns（如 L1 = 文件路径:代码行数）
                    if (lc.ExtraColumns is { Count: > 0 })
                    {
                        foreach (var extra in lc.ExtraColumns)
                        {
                            if (!row.TryGetValue(extra, out var extraVal) || string.IsNullOrWhiteSpace(extraVal))
                                continue;
                            flagValue = $"{flagValue}:{extraVal.Trim()}";
                        }
                    }

                    // 注：按计划"数据集严格按原样导入"，不做 dup 校验。
                    // 重复 L1/L2/L3 值会创建多个 Track，PickFirstValidMatch 匹配第一个，其余不可达——这是数据问题不是平台问题。

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
                        // 规则 R4: 默认 SubmissionLimit=5 (防爆破)
                        var effectiveSubmissionLimit = lc.SubmissionLimit > 0 ? lc.SubmissionLimit : 5;
                        await levelConfigRepo.CreateConfig(new ChallengeLevelConfig
                        {
                            ChallengeId = challengeId,
                            Level = lc.Level,
                            Title = lc.LevelTitle,
                            Score = lc.Score,
                            SubmissionLimit = effectiveSubmissionLimit,
                            PenaltyScore = lc.PenaltyScore,
                            LockOnAnswers = lc.LockOnAnswers,
                            BanOnAnswers = lc.BanOnAnswers,
                            IsEnabled = true,
                            SortOrder = lc.Level
                        }, token);
                    }
                }

                await transaction.CommitAsync(token);
            }
            catch (Exception ex)
            {
                result.Errors.Add(new FlagImportError { Row = idx + 1, Error = ex.Message });
                result.Failed++;
            }
        }

        return result;
    }

    /// <summary>
    /// 按 (仓库, 子模块) 自动建题目并导入 flags。
    /// 每个 (Repository, Submodule) 组合对应一个 GameChallenge：
    /// - 若 GameId 下已存在相同 (Title=子模块, Repository=仓库) 的 GameChallenge，复用之；
    /// - 否则创建新 GameChallenge（Title=子模块, Repository=仓库, Category=Pwn, Type=StaticAttachment, IsEnabled=false）。
    /// 然后对每个 GameChallenge 调 ImportFlagsAsync 创建 Track + LevelConfig + Flag。
    /// </summary>
    public async Task<FlagImportResult> ImportFlagsToGameAsync(
        int gameId,
        Stream fileStream,
        string contentType,
        FlagImportColumnMapping mapping,
        ImportMode mode,
        CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(mapping.RepositoryColumn) || string.IsNullOrWhiteSpace(mapping.SubmoduleColumn))
            throw new ArgumentException("RepositoryColumn and SubmoduleColumn are required for game-level import");

        var rows = ParseFile(fileStream, contentType);
        var result = new FlagImportResult { TotalRows = rows.Count };

        var game = await gameRepo.GetGameById(gameId, token);
        if (game is null)
            throw new ArgumentException($"Game {gameId} not found");

        // 缓存 (Repository, Submodule) → GameChallengeId
        var challengeMap = new Dictionary<(string Repo, string Submod), int>();
        // 跟踪本次导入批次新建的 track ID，用于避免同批次内 L1 重复行被合并
        var batchCreatedTrackIds = new HashSet<Guid>();

        foreach (var (row, idx) in rows.Select((r, i) => (r, i)))
        {
            try
            {
                if (!row.TryGetValue(mapping.RepositoryColumn!, out var repoRaw) || string.IsNullOrWhiteSpace(repoRaw))
                {
                    result.Errors.Add(new FlagImportError { Row = idx + 1, Error = "Repository column is empty" });
                    result.Failed++;
                    continue;
                }
                if (!row.TryGetValue(mapping.SubmoduleColumn!, out var submodRaw) || string.IsNullOrWhiteSpace(submodRaw))
                {
                    result.Errors.Add(new FlagImportError { Row = idx + 1, Error = "Submodule column is empty" });
                    result.Failed++;
                    continue;
                }

                var repo = repoRaw!.Trim();
                var submod = submodRaw!.Trim();
                var key = (repo, submod);

                if (!challengeMap.TryGetValue(key, out var challengeId))
                {
                    // 查找或创建 GameChallenge
                    var existing = await context.GameChallenges
                        .Where(c => c.GameId == gameId && c.Title == submod && c.Repository == repo)
                        .FirstOrDefaultAsync(token);
                    if (existing is not null)
                    {
                        challengeId = existing.Id;
                        result.ReusedChallenges++;
                    }
                    else
                    {
                        var newChal = new GameChallenge
                        {
                            Title = submod,
                            Repository = repo,
                            Category = ChallengeCategory.Pwn,
                            Type = ChallengeType.StaticAttachment,
                            IsEnabled = false,
                            OriginalScore = 100,
                            MinScoreRate = 0.25,
                            Difficulty = 3,
                            GameId = gameId
                        };
                        var created = await challengeRepo.CreateChallenge(game, newChal, token);
                        challengeId = created.Id;
                        result.CreatedChallenges++;
                    }
                    challengeMap[key] = challengeId;
                }

                // 用单题目导入逻辑处理这一行
                await ImportSingleRowAsync(challengeId, row, mapping, mode, idx, result, batchCreatedTrackIds, token);
            }
            catch (Exception ex)
            {
                result.Errors.Add(new FlagImportError { Row = idx + 1, Error = ex.Message });
                result.Failed++;
            }
        }

        return result;
    }

    private async Task<ChallengeTrack?> FindTrackByL1Flag(int challengeId, string l1FlagValue, CancellationToken token)
    {
        // L1 flag value 唯一标识一个 track（因为 L1 匹配是 per-challenge）
        var flags = await flagRepo.GetFlagsByLevel(challengeId, 1, token);
        var matchingFlag = flags.FirstOrDefault(f => f.FlagValue == l1FlagValue);
        if (matchingFlag is null)
            return null;
        return await trackRepo.GetTrack(matchingFlag.TrackId, token);
    }

    private async Task ImportSingleRowAsync(
        int challengeId,
        Dictionary<string, string> row,
        FlagImportColumnMapping mapping,
        ImportMode mode,
        int idx,
        FlagImportResult result,
        HashSet<Guid> batchCreatedTrackIds,
        CancellationToken token)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(token);
        try
        {
            var trackMeta = ExtractTrackMeta(row, mapping);
            var matchValue = mapping.MatchBy == "L1"
                ? ComputeL1FlagValue(row, mapping)
                : GetMatchValue(trackMeta, mapping.MatchBy);

            if (string.IsNullOrWhiteSpace(matchValue))
            {
                result.Errors.Add(new FlagImportError { Row = idx + 1, Error = $"MatchBy column '{mapping.MatchBy}' is empty" });
                result.Failed++;
                await transaction.RollbackAsync(token);
                return;
            }

            // 查找已存在的 track，排除本批次新建的（同批次内 L1 重复行应各自创建独立 track）
            var found = mapping.MatchBy == "L1"
                ? await FindTrackByL1Flag(challengeId, matchValue, token)
                : await FindTrackByMatch(challengeId, mapping.MatchBy, matchValue, token);
            var existingTrack = (found is not null && !batchCreatedTrackIds.Contains(found.Id)) ? found : null;

            if (existingTrack is not null)
            {
                if (mode == ImportMode.Skip)
                {
                    result.Skipped++;
                    await transaction.RollbackAsync(token);
                    return;
                }
                if (mode == ImportMode.Fail)
                {
                    result.Errors.Add(new FlagImportError { Row = idx + 1, Error = "Track exists and mode=Fail" });
                    result.Failed++;
                    await transaction.RollbackAsync(token);
                    return;
                }
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
                batchCreatedTrackIds.Add(track.Id);
                result.Imported++;
            }

            foreach (var lc in mapping.LevelColumns)
            {
                if (!row.TryGetValue(lc.ColumnName, out var answer) || string.IsNullOrWhiteSpace(answer))
                    continue;

                var flagValue = answer.Trim();
                // 拼接 ExtraColumns（如 L1 = 文件路径:代码行数）
                if (lc.ExtraColumns is { Count: > 0 })
                {
                    foreach (var extra in lc.ExtraColumns)
                    {
                        if (!row.TryGetValue(extra, out var extraVal) || string.IsNullOrWhiteSpace(extraVal))
                            continue;
                        flagValue = $"{flagValue}:{extraVal.Trim()}";
                    }
                }
                // 注：按计划"数据集严格按原样导入"，不做 dup 校验。

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

                var levelConfig = await levelConfigRepo.GetConfig(challengeId, lc.Level, token);
                if (levelConfig is null)
                {
                    // 规则 R4: 默认 SubmissionLimit=5 (防爆破)
                    var effectiveSubmissionLimit = lc.SubmissionLimit > 0 ? lc.SubmissionLimit : 5;
                    await levelConfigRepo.CreateConfig(new ChallengeLevelConfig
                    {
                        ChallengeId = challengeId,
                        Level = lc.Level,
                        Title = lc.LevelTitle,
                        Score = lc.Score,
                        SubmissionLimit = effectiveSubmissionLimit,
                        PenaltyScore = lc.PenaltyScore,
                        LockOnAnswers = lc.LockOnAnswers,
                        BanOnAnswers = lc.BanOnAnswers,
                        IsEnabled = true,
                        SortOrder = lc.Level
                    }, token);
                }
            }

            await transaction.CommitAsync(token);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(token);
            result.Errors.Add(new FlagImportError { Row = idx + 1, Error = ex.Message });
            result.Failed++;
        }
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

    /// <summary>
    /// 计算 L1 flag 值（含 ExtraColumns 拼接），用于 "L1" MatchBy。
    /// </summary>
    private static string? ComputeL1FlagValue(Dictionary<string, string> row, FlagImportColumnMapping mapping)
    {
        var lc = mapping.LevelColumns.FirstOrDefault(c => c.Level == 1);
        if (lc is null || string.IsNullOrWhiteSpace(lc.ColumnName))
            return null;
        if (!row.TryGetValue(lc.ColumnName, out var answer) || string.IsNullOrWhiteSpace(answer))
            return null;
        var val = answer.Trim();
        if (lc.ExtraColumns is { Count: > 0 })
        {
            foreach (var extra in lc.ExtraColumns)
            {
                if (!row.TryGetValue(extra, out var extraVal) || string.IsNullOrWhiteSpace(extraVal))
                    continue;
                val = $"{val}:{extraVal.Trim()}";
            }
        }
        return val;
    }
}
