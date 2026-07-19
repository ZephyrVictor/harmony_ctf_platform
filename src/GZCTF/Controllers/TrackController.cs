using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using GZCTF.Middlewares;
using GZCTF.Models;
using GZCTF.Repositories.Interface;
using GZCTF.Services.Scoring;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Controllers;

[ApiController]
[Route("api/ext/challenges/{challengeId:int}/tracks")]
[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
public class TrackController(
    IChallengeTrackRepository trackRepository,
    IChallengeFlagRepository flagRepository,
    IChallengeLevelConfigRepository levelConfigRepository,
    ChallengeScoreService challengeScoreService,
    AppDbContext dbContext) : ControllerBase
{
    [RequireAdmin]
    [HttpGet]
    [ProducesResponseType(typeof(ChallengeTrack[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTracks([FromRoute] int challengeId, CancellationToken token)
    {
        var tracks = await trackRepository.GetTracks(challengeId, token);
        return Ok(tracks);
    }

    [RequireAdmin]
    [HttpPost]
    [ProducesResponseType(typeof(ChallengeTrack), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateTrack([FromRoute] int challengeId,
        [FromBody] TrackCreateModel model, CancellationToken token)
    {
        var track = new ChallengeTrack
        {
            Id = Guid.NewGuid(),
            ChallengeId = challengeId,
            Title = model.Title,
            FlawNo = model.FlawNo,
            Repository = model.Repository,
            Module = model.Module,
            FilePath = model.FilePath,
            Category = model.Category,
            ReferenceUrl = model.ReferenceUrl,
            IsEnabled = model.IsEnabled ?? true,
            SortOrder = model.SortOrder ?? 0
        };

        track = await trackRepository.CreateTrack(track, token);
        await challengeScoreService.SyncOriginalScoreFromLevels(challengeId, token);
        return CreatedAtAction(nameof(GetTrack), new { challengeId, trackId = track.Id }, track);
    }

    /// <summary>
    /// 一次调用创建 Track + 多级 Flag（便捷接口，供管理员在比赛进行中动态添加漏洞）
    /// </summary>
    [RequireAdmin]
    [HttpPost("full")]
    [ProducesResponseType(typeof(TrackFullCreateResult), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateTrackFull([FromRoute] int challengeId,
        [FromBody] TrackFullCreateModel model, CancellationToken token)
    {
        if (model.Levels is null || model.Levels.Count == 0)
            return BadRequest(new RequestResponse("At least one level is required"));

        await using var transaction = await dbContext.Database.BeginTransactionAsync(token);
        try
        {
            var trackModel = model.Track ?? new TrackCreateModel();
            var track = new ChallengeTrack
            {
                Id = Guid.NewGuid(),
                ChallengeId = challengeId,
                Title = trackModel.Title,
                FlawNo = trackModel.FlawNo,
                Repository = trackModel.Repository,
                Module = trackModel.Module,
                FilePath = trackModel.FilePath,
                Category = trackModel.Category,
                ReferenceUrl = trackModel.ReferenceUrl,
                IsEnabled = trackModel.IsEnabled ?? true,
                SortOrder = trackModel.SortOrder ?? 0
            };
            track = await trackRepository.CreateTrack(track, token);

            var createdFlags = new List<ChallengeFlag>();
            foreach (var lvl in model.Levels)
            {
                if (string.IsNullOrWhiteSpace(lvl.FlagValue))
                {
                    await transaction.RollbackAsync(token);
                    return BadRequest(new RequestResponse($"FlagValue for level {lvl.Level} is required"));
                }

                var flagValue = lvl.FlagValue.Trim();
                // 注：按计划"数据集严格按原样导入"，不做 dup 校验。

                var flag = new ChallengeFlag
                {
                    Id = Guid.NewGuid(),
                    ChallengeId = challengeId,
                    FlagValue = flagValue,
                    AnswerType = lvl.AnswerType,
                    RawAnswer = flagValue,
                    NormalizedAnswerKey = ScoringService.NormalizeHybridLineAnswer(flagValue, lvl.AnswerType),
                    Level = lvl.Level,
                    TrackId = track.Id,
                    IsEnabled = true,
                    IsVisible = true,
                    SortOrder = 0
                };
                createdFlags.Add(await flagRepository.CreateFlag(flag, token));

                // Upsert LevelConfig
                var levelConfig = await levelConfigRepository.GetConfig(challengeId, lvl.Level, token);
                if (levelConfig is null)
                {
                    await levelConfigRepository.CreateConfig(new ChallengeLevelConfig
                    {
                        ChallengeId = challengeId,
                        Level = lvl.Level,
                        Title = lvl.LevelTitle,
                        Score = lvl.Score,
                        PenaltyScore = lvl.PenaltyScore,
                        LockOnAnswers = lvl.LockOnAnswers,
                        BanOnAnswers = lvl.BanOnAnswers,
                        CaseSensitive = lvl.CaseSensitive,
                        PathCaseSensitive = lvl.PathCaseSensitive,
                        OrSeparators = string.IsNullOrEmpty(lvl.OrSeparators) ? "、,;" : lvl.OrSeparators,
                        RangeSeparator = string.IsNullOrWhiteSpace(lvl.RangeSeparator) ? "-" : lvl.RangeSeparator,
                        StripLinePrefix = lvl.StripLinePrefix,
                        IsEnabled = true,
                        SortOrder = lvl.Level
                    }, token);
                }
            }

            await transaction.CommitAsync(token);
            await challengeScoreService.SyncOriginalScoreFromLevels(challengeId, token);
            return CreatedAtAction(nameof(GetTrack), new { challengeId, trackId = track.Id },
                new TrackFullCreateResult { Track = track, Flags = createdFlags });
        }
        catch
        {
            await transaction.RollbackAsync(token);
            throw;
        }
    }

    [RequireAdmin]
    [HttpGet("{trackId:guid}")]
    [ProducesResponseType(typeof(ChallengeTrack), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTrack([FromRoute] int challengeId, [FromRoute] Guid trackId, CancellationToken token)
    {
        var track = await trackRepository.GetTrack(trackId, token);
        if (track is null || track.ChallengeId != challengeId)
            return NotFound(new RequestResponse("Track not found"));
        return Ok(track);
    }

    [RequireAdmin]
    [HttpPut("{trackId:guid}")]
    [ProducesResponseType(typeof(ChallengeTrack), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateTrack([FromRoute] int challengeId, [FromRoute] Guid trackId,
        [FromBody] TrackUpdateModel model, CancellationToken token)
    {
        var track = await trackRepository.GetTrack(trackId, token);
        if (track is null || track.ChallengeId != challengeId)
            return NotFound(new RequestResponse("Track not found"));

        if (model.Title is not null) track.Title = model.Title;
        if (model.FlawNo is not null) track.FlawNo = model.FlawNo;
        if (model.Repository is not null) track.Repository = model.Repository;
        if (model.Module is not null) track.Module = model.Module;
        if (model.FilePath is not null) track.FilePath = model.FilePath;
        if (model.Category is not null) track.Category = model.Category;
        if (model.ReferenceUrl is not null) track.ReferenceUrl = model.ReferenceUrl;
        if (model.IsEnabled.HasValue) track.IsEnabled = model.IsEnabled.Value;
        if (model.SortOrder.HasValue) track.SortOrder = model.SortOrder.Value;

        track = await trackRepository.UpdateTrack(track, token);
        await challengeScoreService.SyncOriginalScoreFromLevels(challengeId, token);
        return Ok(track);
    }

    [RequireAdmin]
    [HttpDelete("{trackId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteTrack([FromRoute] int challengeId, [FromRoute] Guid trackId, CancellationToken token)
    {
        var track = await trackRepository.GetTrack(trackId, token);
        if (track is null || track.ChallengeId != challengeId)
            return NotFound(new RequestResponse("Track not found"));

        await trackRepository.DeleteTrack(trackId, token);
        await challengeScoreService.SyncOriginalScoreFromLevels(challengeId, token);
        return NoContent();
    }

}

public class TrackCreateModel
{
    [MaxLength(200)]
    public string? Title { get; set; }

    [MaxLength(64)]
    public string? FlawNo { get; set; }

    [MaxLength(128)]
    public string? Repository { get; set; }

    [MaxLength(128)]
    public string? Module { get; set; }

    [MaxLength(512)]
    public string? FilePath { get; set; }

    [MaxLength(128)]
    public string? Category { get; set; }

    [MaxLength(512)]
    public string? ReferenceUrl { get; set; }

    public bool? IsEnabled { get; set; }

    [Range(0, int.MaxValue)]
    public int? SortOrder { get; set; }
}

public class TrackUpdateModel
{
    [MaxLength(200)]
    public string? Title { get; set; }

    [MaxLength(64)]
    public string? FlawNo { get; set; }

    [MaxLength(128)]
    public string? Repository { get; set; }

    [MaxLength(128)]
    public string? Module { get; set; }

    [MaxLength(512)]
    public string? FilePath { get; set; }

    [MaxLength(128)]
    public string? Category { get; set; }

    [MaxLength(512)]
    public string? ReferenceUrl { get; set; }

    public bool? IsEnabled { get; set; }

    [Range(0, int.MaxValue)]
    public int? SortOrder { get; set; }
}

public class TrackFullCreateModel
{
    public TrackCreateModel? Track { get; set; }
    public List<TrackFullLevelModel> Levels { get; set; } = [];
}

public class TrackFullLevelModel
{
    [Range(1, int.MaxValue)]
    public int Level { get; set; }

    [Required]
    [MaxLength(Limits.MaxFlagLength)]
    public string FlagValue { get; set; } = string.Empty;

    public ChallengeFlagAnswerType AnswerType { get; set; } = ChallengeFlagAnswerType.Exact;

    /// <summary>
    /// 该级别分值（默认 L1=5 / L2=4 / L3=1，其余 = 0）
    /// </summary>
    public int Score { get; set; }

    public int PenaltyScore { get; set; }
    public List<string> LockOnAnswers { get; set; } = [];
    public List<string> BanOnAnswers { get; set; } = [];
    public string? LevelTitle { get; set; }
    public bool CaseSensitive { get; set; } = true;
    public bool PathCaseSensitive { get; set; } = true;
    public string OrSeparators { get; set; } = "、,;";
    public string RangeSeparator { get; set; } = "-";
    public bool StripLinePrefix { get; set; } = true;
}

public class TrackFullCreateResult
{
    public ChallengeTrack Track { get; set; } = null!;
    public List<ChallengeFlag> Flags { get; set; } = [];
}
