using System.IO.Compression;
using GZCTF.Models.Request.Edit;
using GZCTF.Models.Transfer;
using GZCTF.Repositories.Interface;
using GZCTF.Storage.Interface;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Services.Transfer;

/// <summary>
/// Game import service
/// </summary>
public class GameImportService(
    IGameRepository gameRepository,
    IGameChallengeRepository challengeRepository,
    IDivisionRepository divisionRepository,
    IChallengeTrackRepository trackRepository,
    IChallengeLevelConfigRepository levelConfigRepository,
    IBlobRepository blobRepository,
    IBlobStorage blobStorage,
    AppDbContext dbContext,
    ILogger<GameImportService> logger)
{
    /// <summary>
    /// Import context containing parsed data, ID mappings, and uploaded files tracking
    /// </summary>
    internal record ImportContext(
        TransferManifest Manifest,
        TransferGame Game,
        List<TransferChallenge> Challenges,
        string WorkDir,
        Dictionary<int, int> ChallengeIdMap,
        Dictionary<int, int> DivisionIdMap,
        HashSet<string> UploadedFiles);

    /// <summary>
    /// Import game from ZIP file
    /// </summary>
    /// <param name="zipStream">ZIP file stream</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Imported game ID, or null if import failed</returns>
    public async Task<int?> ImportGameAsync(Stream zipStream, CancellationToken ct = default)
    {
        // Create temporary working directory
        var workDir = Path.Combine(Path.GetTempPath(), $"gzctf-import-{Guid.NewGuid()}");
        Directory.CreateDirectory(workDir);

        try
        {
            // Extract ZIP package
            await ExtractPackageAsync(zipStream, workDir, ct);

            // Parse and validate package contents
            var context = await ParsePackageAsync(workDir, ct);

            // Verify file integrity
            await VerifyFilesAsync(context, ct);

            // Import to database with transaction
            var gameId = await ImportToDatabaseAsync(context, ct);

            return gameId;
        }
        catch (Exception ex)
        {
            logger.SystemLog(ex.Message, TaskStatus.Failed, LogLevel.Error);
            throw;
        }
        finally
        {
            // Clean up temporary directory
            if (Directory.Exists(workDir))
            {
                try
                {
                    Directory.Delete(workDir, recursive: true);
                }
                catch (Exception ex)
                {
                    logger.SystemLog(
                        $"Failed to clean up import directory at {workDir}: {ex.Message}",
                        TaskStatus.Failed,
                        LogLevel.Warning);
                }
            }
        }
    }

    /// <summary>
    /// Extract ZIP package to working directory
    /// </summary>
    private static async Task ExtractPackageAsync(Stream zipStream, string workDir, CancellationToken ct)
    {
        // Save stream to temporary file for extraction
        var tempZipPath = Path.Combine(workDir, "temp.zip");
        await using (var fileStream = File.Create(tempZipPath))
        {
            await zipStream.CopyToAsync(fileStream, ct);
        }

        try
        {
            // Extract ZIP
            await ZipFile.ExtractToDirectoryAsync(tempZipPath, workDir, ct);
        }
        catch (InvalidDataException ex)
        {
            throw new InvalidOperationException("Invalid or corrupted ZIP file", ex);
        }
        finally
        {
            // Always delete temporary ZIP file
            if (File.Exists(tempZipPath))
                File.Delete(tempZipPath);
        }
    }

    /// <summary>
    /// Parse and validate package contents
    /// </summary>
    private static async Task<ImportContext> ParsePackageAsync(string workDir, CancellationToken ct)
    {
        // Validate manifest
        var manifestPath = Path.Combine(workDir, "manifest.json");
        if (!File.Exists(manifestPath))
            throw new InvalidOperationException("Missing manifest.json in import package");

        var manifestJson = await File.ReadAllTextAsync(manifestPath, ct);
        var manifest = TransferHelper.FromJson<TransferManifest>(manifestJson)
                       ?? throw new InvalidOperationException("Invalid manifest.json format");

        // Validate format
        if (manifest.Format != "GZCTF-GAME")
            throw new InvalidOperationException($"Unsupported package format: {manifest.Format}");

        // Parse game.json
        var gamePath = Path.Combine(workDir, "game.json");
        if (!File.Exists(gamePath))
            throw new InvalidOperationException("Missing game.json in import package");

        var gameJson = await File.ReadAllTextAsync(gamePath, ct);
        var game = TransferHelper.FromJson<TransferGame>(gameJson)
                   ?? throw new InvalidOperationException("Invalid game.json format");

        // Parse challenges
        var challengesDir = Path.Combine(workDir, "challenges");
        if (!Directory.Exists(challengesDir))
            throw new InvalidOperationException("Missing challenges directory in import package");

        var challengeFiles = Directory.GetFiles(challengesDir, "challenge-*.json");
        var challenges = new List<TransferChallenge>();

        foreach (var challengeFile in challengeFiles)
        {
            var challengeJson = await File.ReadAllTextAsync(challengeFile, ct);
            var challenge = TransferHelper.FromJson<TransferChallenge>(challengeJson)
                            ?? throw new InvalidOperationException($"Invalid challenge file: {challengeFile}");
            challenges.Add(challenge);
        }

        return new ImportContext(
            Manifest: manifest,
            Game: game,
            Challenges: challenges,
            WorkDir: workDir,
            ChallengeIdMap: new Dictionary<int, int>(),
            DivisionIdMap: new Dictionary<int, int>(),
            UploadedFiles: []);
    }

    /// <summary>
    /// Verify file integrity by checking hashes
    /// </summary>
    private async Task VerifyFilesAsync(ImportContext context, CancellationToken ct)
    {
        var filesDir = Path.Combine(context.WorkDir, "files");
        if (!Directory.Exists(filesDir))
            return;

        var files = Directory.GetFiles(filesDir);

        foreach (var file in files)
        {
            var expectedHash = Path.GetFileName(file);
            var computedHash = await TransferHelper.ComputeFileHashAsync(file, ct);

            if (!computedHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"File integrity check failed for '{expectedHash}': " +
                    $"computed hash '{computedHash}' does not match filename");
        }
    }

    /// <summary>
    /// Import data to database with transaction and atomicity guarantee
    /// </summary>
    private async Task<int> ImportToDatabaseAsync(ImportContext context, CancellationToken ct)
    {
        TransferValidator.ValidateRecursive(context.Game, "Game");
        foreach (var challenge in context.Challenges)
        {
            TransferValidator.ValidateRecursive(challenge, $"Challenge '{challenge.Title}'");
        }

        // Use transaction to ensure atomicity
        await using var transaction = await gameRepository.BeginTransactionAsync(ct);

        try
        {
            // Create new game
            var game = new Game
            {
                Title = context.Game.Title,
                Summary = context.Game.Summary,
                Content = context.Game.Content,
                StartTimeUtc = context.Game.StartTime,
                EndTimeUtc = context.Game.EndTime,
                WriteupRequired = context.Game.Writeup?.Required ?? false,
                WriteupDeadline = context.Game.Writeup?.Deadline ?? DateTimeOffset.FromUnixTimeSeconds(0),
                WriteupNote = context.Game.Writeup?.Note ?? string.Empty,
                AcceptWithoutReview = context.Game.AcceptWithoutReview,
                TeamMemberCountLimit = context.Game.TeamMemberCountLimit,
                ContainerCountLimit = context.Game.ContainerCountLimit,
                InviteCode = string.Empty, // Generate new invite code
                PracticeMode = false,
                Hidden = true // Import as hidden by default
            };

            // Import blood bonus configuration
            if (context.Game.BloodBonus is not null)
            {
                var bonus = context.Game.BloodBonus;
                // BloodBonus values are already validated by TransferValidator (0-1023 range)
                game.BloodBonusValue = ((long)bonus.First << 20) + ((long)bonus.Second << 10) + bonus.Third;
            }

            // Import poster if exists
            if (!string.IsNullOrWhiteSpace(context.Game.PosterHash))
            {
                await ImportFileAsync(context.Game.PosterHash, context, ct, "poster");
                game.PosterHash = context.Game.PosterHash;
            }

            // Create game in database
            game = await gameRepository.CreateGame(game, ct)
                   ?? throw new InvalidOperationException("Failed to create game in database");

            // Note: CreateGame automatically generates PublicKey/PrivateKey via game.GenerateKeyPair(_xorKey)

            // Import divisions (basic info only, permissions will be set after challenges)
            await ImportDivisionsAsync(game, context, ct);

            // Import challenges
            foreach (var transferChallenge in context.Challenges)
            {
                var challenge = await ImportChallengeAsync(game, transferChallenge, context, ct);
                context.ChallengeIdMap[transferChallenge.Id] = challenge.Id;
            }

            // Import division permissions and challenge-specific configurations
            await ImportDivisionPermissionsAsync(game, context, ct);

            // Commit transaction
            await transaction.CommitAsync(ct);

            return game.Id;
        }
        catch
        {
            // Rollback transaction
            await transaction.RollbackAsync(ct);

            // Clean up uploaded files
            await CleanupUploadedFilesAsync(context, ct);

            throw;
        }
    }

    /// <summary>
    /// Import divisions for the game (basic info only)
    /// </summary>
    private async Task ImportDivisionsAsync(Game game, ImportContext context, CancellationToken ct)
    {
        if (context.Game.Divisions.Count == 0)
            return;

        foreach (var transferDivision in context.Game.Divisions)
        {
            var division = await divisionRepository.CreateDivision(game,
                new DivisionCreateModel { Name = transferDivision.Name }, ct);

            // Track original ID to new database ID mapping (using array index as original ID)
            var originalId = context.Game.Divisions.IndexOf(transferDivision);
            context.DivisionIdMap[originalId] = division.Id;
        }
    }

    /// <summary>
    /// Import division permissions and challenge-specific configurations
    /// </summary>
    /// <remarks>
    /// Must be called after challenges are imported to map challenge IDs
    /// </remarks>
    private async Task ImportDivisionPermissionsAsync(Game game, ImportContext context, CancellationToken ct)
    {
        if (context.Game.Divisions.Count == 0)
            return;

        foreach (var transferDivision in context.Game.Divisions)
        {
            var originalId = context.Game.Divisions.IndexOf(transferDivision);
            if (!context.DivisionIdMap.TryGetValue(originalId, out var divisionId))
                continue;

            var division = await divisionRepository.GetDivision(game.Id, divisionId, ct);
            if (division is null)
                continue;

            // Parse default permissions from string flags
            var defaultPermissions = TransferExtensions.StringsToPermissions(transferDivision.DefaultPermissions);

            // Build challenge configs with mapped IDs
            var challengeConfigs = new HashSet<DivisionChallengeConfigModel>();
            foreach (var config in transferDivision.ChallengeConfig
                         .Where(cfg => context.ChallengeIdMap.ContainsKey(cfg.ChallengeId)))
            {
                var newChallengeId = context.ChallengeIdMap[config.ChallengeId];
                challengeConfigs.Add(new DivisionChallengeConfigModel
                {
                    ChallengeId = newChallengeId,
                    Permissions = TransferExtensions.StringsToPermissions(config.Permissions)
                });
            }

            // Update division with full configuration
            await divisionRepository.UpdateDivision(division,
                new DivisionEditModel
                {
                    Name = transferDivision.Name,
                    InviteCode = transferDivision.InviteCode,
                    DefaultPermissions = defaultPermissions,
                    ChallengeConfigs = challengeConfigs
                }, ct);
        }
    }

    /// <summary>
    /// Clean up newly uploaded files in case of import failure
    /// </summary>
    /// <remarks>
    /// Only files in UploadedFiles (newly uploaded, not previously existing) are cleaned up.
    /// For files that already existed before import, database rollback will decrease their reference count automatically.
    /// Since database rollback removes the LocalFile records for newly uploaded files, we must manually delete the physical files.
    /// </remarks>
    private async Task CleanupUploadedFilesAsync(ImportContext context, CancellationToken ct)
    {
        foreach (var hash in context.UploadedFiles)
        {
            try
            {
                // After database rollback, LocalFile records for newly uploaded files are gone
                // We need to delete the physical files directly from storage
                var path = StoragePath.Combine(PathHelper.Uploads, hash[..2], hash);

                await blobStorage.DeleteAsync(path, ct);
            }
            catch (Exception ex)
            {
                logger.SystemLog(
                    $"Failed to delete uploaded file {hash[..8]} during cleanup: {ex.Message}",
                    TaskStatus.Failed,
                    LogLevel.Warning);
            }
        }
    }

    /// <summary>
    /// Import a single challenge
    /// </summary>
    private async Task<GameChallenge> ImportChallengeAsync(
        Game game,
        TransferChallenge transferChallenge,
        ImportContext context,
        CancellationToken ct)
    {
        var challenge = new GameChallenge
        {
            Title = transferChallenge.Title,
            Content = transferChallenge.Content,
            Category = transferChallenge.Category,
            Type = transferChallenge.Type,
            IsEnabled = false, // Import as disabled by default
            GameId = game.Id,

            // Scoring
            OriginalScore = transferChallenge.Scoring.Original,
            MinScoreRate = transferChallenge.Scoring.MinRate,
            Difficulty = transferChallenge.Scoring.Difficulty,

            // Limits
            DeadlineUtc = transferChallenge.Limits.Deadline,

            // Flags configuration
            FlagTemplate = transferChallenge.Flags.Template,
            DisableBloodBonus = transferChallenge.Flags.DisableBloodBonus,
            EnableTrafficCapture = transferChallenge.Flags.EnableTrafficCapture,

            // Container settings
            ContainerImage = transferChallenge.Container?.Image,
            ExposePort = transferChallenge.Container?.ExposePort,
            MemoryLimit = transferChallenge.Container?.MemoryLimit,
            CPUCount = transferChallenge.Container?.CpuCount,
            StorageLimit = transferChallenge.Container?.StorageLimit,
            NetworkMode = transferChallenge.Container?.NetworkMode,

            // File settings
            FileName = transferChallenge.Container?.FileName,

            // Hints
            Hints = transferChallenge.Hints
        };

        // Create challenge
        challenge = await challengeRepository.CreateChallenge(game, challenge, ct);

        // Import attachment if exists
        if (transferChallenge.Attachment is not null)
        {
            await ImportAttachmentAsync(challenge, transferChallenge.Attachment, context, ct);
        }

        // Import flags
        await ImportFlagsAsync(challenge, transferChallenge.Flags, context, ct);

        return challenge;
    }

    /// <summary>
    /// Import challenge attachment
    /// </summary>
    private async Task ImportAttachmentAsync(
        GameChallenge challenge,
        AttachmentSection attachment,
        ImportContext context,
        CancellationToken ct)
    {
        switch (attachment.Type)
        {
            case FileType.Local:
                if (string.IsNullOrWhiteSpace(attachment.Hash))
                    throw new InvalidOperationException("Local attachment missing file hash");

                // Import file to blob storage
                await ImportFileAsync(attachment.Hash, context, ct, attachment.FileName);

                // Create attachment record with local file
                await challengeRepository.UpdateAttachment(challenge,
                    new AttachmentCreateModel { AttachmentType = FileType.Local, FileHash = attachment.Hash }, ct);
                break;

            case FileType.Remote:
                if (string.IsNullOrWhiteSpace(attachment.RemoteUrl))
                    throw new InvalidOperationException("Remote attachment missing URL");

                // Create attachment record with remote URL (no file import needed)
                await challengeRepository.UpdateAttachment(challenge,
                    new AttachmentCreateModel { AttachmentType = FileType.Remote, RemoteUrl = attachment.RemoteUrl },
                    ct);
                break;

            case FileType.None:
                // No attachment, skip
                break;

            default:
                throw new InvalidOperationException($"Unsupported attachment type: {attachment.Type}");
        }
    }

    /// <summary>
    /// Import flags for a challenge.
    /// 支持两种格式：
    ///   1. MultiLevel（新格式）：完整保留 Track + LevelConfig + Flag 三层结构
    ///   2. Static（老格式）：按 D3 警告 + 合成单 Track + L1 LevelConfig
    /// </summary>
    private async Task ImportFlagsAsync(
        GameChallenge challenge,
        FlagsSection flags,
        ImportContext context,
        CancellationToken ct)
    {
        // 新格式优先
        if (flags.MultiLevel is not null)
        {
            await ImportMultiLevelFlagsAsync(challenge, flags.MultiLevel, context, ct);
            return;
        }

        // 老格式：Static 列表（D3 警告 + 合成）
        if (flags.Static is null || flags.Static.Count == 0)
            return;

        logger.LogWarning(
            "Transfer file uses legacy flag format for challenge {ChallengeId}, synthesizing single-track L1 structure",
            challenge.Id);

        await ImportLegacyStaticFlagsAsync(challenge, flags.Static, context, ct);
    }

    /// <summary>
    /// 导入新格式多级 flag（Track + LevelConfig + Flag 三层）
    /// </summary>
    private async Task ImportMultiLevelFlagsAsync(
        GameChallenge challenge,
        MultiLevelFlagsSection multiLevel,
        ImportContext context,
        CancellationToken ct)
    {
        // 1. 创建 Tracks（保留原 ID）
        var trackIdMap = new Dictionary<Guid, Guid>(); // 原 ID → 新 ID（此处保留原 ID）
        foreach (var transferTrack in multiLevel.Tracks)
        {
            var track = new ChallengeTrack
            {
                Id = transferTrack.Id, // 保留原 ID 以便 Flag 关联
                ChallengeId = challenge.Id,
                Title = transferTrack.Title,
                FlawNo = transferTrack.FlawNo,
                Repository = transferTrack.Repository,
                Module = transferTrack.Module,
                FilePath = transferTrack.FilePath,
                Category = transferTrack.Category,
                ReferenceUrl = transferTrack.ReferenceUrl,
                IsEnabled = transferTrack.IsEnabled,
                SortOrder = transferTrack.SortOrder
            };

            await trackRepository.CreateTrack(track, ct);
            trackIdMap[transferTrack.Id] = track.Id;
        }

        // 2. 创建 LevelConfigs
        foreach (var transferLevel in multiLevel.LevelConfigs)
        {
            var levelConfig = new ChallengeLevelConfig
            {
                ChallengeId = challenge.Id,
                Level = transferLevel.Level,
                Title = transferLevel.Title,
                Score = transferLevel.Score,
                PenaltyScore = transferLevel.PenaltyScore,
                LockOnAnswers = transferLevel.LockOnAnswers ?? [],
                BanOnAnswers = transferLevel.BanOnAnswers ?? [],
                IsEnabled = transferLevel.IsEnabled,
                SortOrder = transferLevel.SortOrder
            };

            await levelConfigRepository.CreateConfig(levelConfig, ct);
        }

        // 3. 创建 Flags（TrackId 指向新创建的 Track）
        foreach (var transferFlag in multiLevel.Flags)
        {
            Attachment? attachment = null;
            if (transferFlag.Attachment is not null)
            {
                attachment = await ImportFlagAttachmentAsync(transferFlag.Attachment, context, ct);
            }

            var flag = new ChallengeFlag
            {
                Id = transferFlag.Id,
                FlagValue = transferFlag.FlagValue,
                AnswerType = transferFlag.AnswerType,
                RawAnswer = transferFlag.RawAnswer,
                // 归一化答案键由 ChallengeFlagRepository 或 SaveAsync 时计算；
                // 这里使用与 ExtController.CreateFlag 相同的简单赋值策略
                NormalizedAnswerKey = transferFlag.RawAnswer,
                Level = transferFlag.Level,
                TrackId = transferFlag.TrackId,
                IsEnabled = transferFlag.IsEnabled,
                IsVisible = transferFlag.IsVisible,
                IsOccupied = transferFlag.IsOccupied,
                SortOrder = transferFlag.SortOrder,
                ChallengeId = challenge.Id,
                Attachment = attachment
            };

            challenge.Flags.Add(flag);
        }

        await dbContext.SaveChangesAsync(ct);
    }

    /// <summary>
    /// 导入老格式 Static flags（D3：警告 + 合成单 Track + L1 LevelConfig）
    /// </summary>
    private async Task ImportLegacyStaticFlagsAsync(
        GameChallenge challenge,
        List<StaticFlagSection> staticFlags,
        ImportContext context,
        CancellationToken ct)
    {
        // 为每个 static flag 创建独立 Track + L1 LevelConfig + Flag
        var sortOrder = 0;
        foreach (var staticFlag in staticFlags)
        {
            var trackId = Guid.NewGuid();
            var track = new ChallengeTrack
            {
                Id = trackId,
                ChallengeId = challenge.Id,
                Title = $"Imported Track {sortOrder + 1}",
                IsEnabled = true,
                SortOrder = sortOrder
            };
            await trackRepository.CreateTrack(track, ct);

            // 仅当该 challenge 还没有 L1 LevelConfig 时创建（多个 static flag 共享 L1 config）
            var existingL1Config = await levelConfigRepository.GetConfig(challenge.Id, 1, ct);
            if (existingL1Config is null)
            {
                var l1Config = new ChallengeLevelConfig
                {
                    ChallengeId = challenge.Id,
                    Level = 1,
                    Title = "Level 1",
                    Score = challenge.OriginalScore,
                    PenaltyScore = 0,
                    IsEnabled = true,
                    SortOrder = 0
                };
                await levelConfigRepository.CreateConfig(l1Config, ct);
            }

            // 创建 Flag
            Attachment? attachment = null;
            if (staticFlag.Attachment is not null)
            {
                attachment = await ImportFlagAttachmentAsync(staticFlag.Attachment, context, ct);
            }

            challenge.Flags.Add(new ChallengeFlag
            {
                FlagValue = staticFlag.Value,
                AnswerType = ChallengeFlagAnswerType.Exact,
                RawAnswer = staticFlag.Value,
                NormalizedAnswerKey = staticFlag.Value,
                Level = 1,
                TrackId = trackId,
                IsEnabled = true,
                IsVisible = true,
                IsOccupied = false,
                SortOrder = sortOrder,
                ChallengeId = challenge.Id,
                Attachment = attachment
            });

            sortOrder++;
        }

        await dbContext.SaveChangesAsync(ct);
    }

    /// <summary>
    /// 导入 flag 附件（Local / Remote），返回 Attachment 实体
    /// </summary>
    private async Task<Attachment?> ImportFlagAttachmentAsync(
        AttachmentSection attachmentSection,
        ImportContext context,
        CancellationToken ct)
    {
        switch (attachmentSection.Type)
        {
            case FileType.Local:
                if (string.IsNullOrWhiteSpace(attachmentSection.Hash))
                    throw new InvalidOperationException("Local flag attachment missing file hash");

                await ImportFileAsync(attachmentSection.Hash, context, ct, attachmentSection.FileName);

                var localFile = await blobRepository.GetBlobByHash(attachmentSection.Hash, ct);
                return new Attachment
                {
                    Type = FileType.Local,
                    LocalFile = localFile
                };

            case FileType.Remote:
                if (string.IsNullOrWhiteSpace(attachmentSection.RemoteUrl))
                    throw new InvalidOperationException("Remote flag attachment missing URL");

                return new Attachment
                {
                    Type = FileType.Remote,
                    RemoteUrl = attachmentSection.RemoteUrl
                };

            case FileType.None:
                return null;

            default:
                throw new InvalidOperationException($"Unsupported flag attachment type: {attachmentSection.Type}");
        }
    }

    /// <summary>
    /// Import a file to blob storage and track it for potential cleanup
    /// </summary>
    private async Task ImportFileAsync(string hash, ImportContext context, CancellationToken ct,
        string? fileName = null)
    {
        var filePath = Path.Combine(context.WorkDir, "files", hash);
        if (!File.Exists(filePath))
            throw new InvalidOperationException($"Missing file in package: {hash}");

        // Check if file already exists in database
        var existingFile = await blobRepository.GetBlobByHash(hash, ct);

        if (existingFile is not null)
        {
            // File already exists, only increment reference count
            // If import fails, database rollback will handle the ref count decrease
            await blobRepository.IncrementBlobReference(hash, ct);
        }
        else
        {
            // File doesn't exist, upload new file
            // These newly uploaded files need to be tracked for cleanup on failure
            await using var fileStream = File.OpenRead(filePath);
            var localFile = await blobRepository.CreateOrUpdateBlobFromStream(fileName ?? hash, fileStream, ct);

            // Track newly uploaded file hash for cleanup on failure
            // Database rollback will remove the LocalFile record, but physical file needs manual deletion
            context.UploadedFiles.Add(localFile.Hash);
        }
    }
}
