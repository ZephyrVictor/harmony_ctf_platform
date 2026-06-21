using System.Net.Mime;
using System.Text.Json;
using GZCTF.Middlewares;
using GZCTF.Models;
using GZCTF.Models.Request;
using GZCTF.Repositories.Interface;
using GZCTF.Services.Cache;
using GZCTF.Services.Scoring;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GZCTF.Controllers;

/// <summary>
/// Extended APIs for flag scoring, penalty, and competition settings
/// </summary>
[ApiController]
[Route("api/ext")]
[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
public class ExtController(
    IChallengeFlagRepository flagRepository,
    IChallengePenaltyRepository penaltyRepository,
    CompetitionSettingsService settingsService,
    IBigScreenEventRepository eventRepository,
    AppDbContext context,
    CacheHelper cacheHelper,
    IChallengeTrackRepository trackRepository,
    IChallengeLevelConfigRepository levelConfigRepository) : ControllerBase
{
    private async Task FlushScoreboardForChallenge(int challengeId, CancellationToken token)
    {
        var gameId = await context.GameChallenges
            .Where(c => c.Id == challengeId)
            .Select(c => c.GameId)
            .FirstOrDefaultAsync(token);

        if (gameId > 0)
            await cacheHelper.FlushScoreboardCache(gameId, token);
    }

    #region Flag Management

    /// <summary>
    /// Get all flags for a challenge
    /// </summary>
    [RequireAdmin]
    [HttpGet("challenges/{challengeId:int}/flags")]
    [ProducesResponseType(typeof(ChallengeFlag[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFlags([FromRoute] int challengeId, CancellationToken token)
    {
        var flags = await flagRepository.GetFlags(challengeId, token);
        return Ok(flags);
    }

    /// <summary>
    /// Get a single flag by ID
    /// </summary>
    [RequireAdmin]
    [HttpGet("challenges/{challengeId:int}/flags/{flagId:guid}")]
    [ProducesResponseType(typeof(ChallengeFlag), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFlag([FromRoute] int challengeId, [FromRoute] Guid flagId,
        CancellationToken token)
    {
        var flag = await flagRepository.GetFlag(flagId, token);
        if (flag is null || flag.ChallengeId != challengeId)
            return NotFound(new RequestResponse("Flag not found"));
        return Ok(flag);
    }

    /// <summary>
    /// Create a new flag for a challenge
    /// </summary>
    [RequireAdmin]
    [HttpPost("challenges/{challengeId:int}/flags")]
    [ProducesResponseType(typeof(ChallengeFlag), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateFlag([FromRoute] int challengeId,
        [FromBody] ChallengeFlagCreateModel model, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(model.FlagValue))
            return BadRequest(new RequestResponse("Flag value is required"));

        if (model.FlagValue.Length > Limits.MaxFlagLength)
            return BadRequest(new RequestResponse("Flag value is too long"));

        var level = model.Level <= 0 ? 1 : model.Level;
        var trackId = model.TrackId ?? Guid.NewGuid();

        // Validate track exists and belongs to this challenge (when explicitly provided)
        if (model.TrackId.HasValue)
        {
            var track = await trackRepository.GetTrack(model.TrackId.Value, token);
            if (track is null || track.ChallengeId != challengeId)
                return BadRequest(new RequestResponse("Track does not belong to this challenge"));
        }
        else
        {
            // No explicit track: synthesize one so the flag is visible in player view
            var synthTrack = new ChallengeTrack
            {
                Id = trackId,
                ChallengeId = challengeId,
                Title = $"Track for Level {level}",
                IsEnabled = true,
                SortOrder = 0
            };
            await trackRepository.CreateTrack(synthTrack, token);
        }

        // Validate level config exists (when level > 1, must have config)
        if (level > 1)
        {
            var levelConfig = await levelConfigRepository.GetConfig(challengeId, level, token);
            if (levelConfig is null)
                return BadRequest(new RequestResponse($"Level {level} config does not exist"));
        }

        var exists = await flagRepository.ExistsByValue(challengeId, model.FlagValue, token);
        if (exists)
            return BadRequest(new RequestResponse("This flag value already exists for this challenge"));

        var flag = new ChallengeFlag
        {
            ChallengeId = challengeId,
            FlagValue = model.FlagValue,
            AnswerType = model.AnswerType,
            RawAnswer = string.IsNullOrWhiteSpace(model.RawAnswer) ? model.FlagValue : model.RawAnswer,
            IsVisible = model.IsVisible,
            IsEnabled = model.IsEnabled,
            Level = level,
            TrackId = trackId,
            SortOrder = model.SortOrder
        };

        if (!TrySetNormalizedAnswerKey(flag, out var error))
            return BadRequest(error!);

        if (await HasNormalizedAnswerConflict(challengeId, flag.NormalizedAnswerKey, null, token))
            return BadRequest(new RequestResponse("This normalized answer already exists for this challenge"));

        flag = await flagRepository.CreateFlag(flag, token);
        await FlushScoreboardForChallenge(challengeId, token);
        return CreatedAtAction(nameof(GetFlags), new { challengeId }, flag);
    }

    /// <summary>
    /// Update an existing flag
    /// </summary>
    [RequireAdmin]
    [HttpPut("challenges/{challengeId:int}/flags/{flagId:guid}")]
    [ProducesResponseType(typeof(ChallengeFlag), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateFlag([FromRoute] int challengeId, [FromRoute] Guid flagId,
        [FromBody] ChallengeFlagUpdateModel model, CancellationToken token)
    {
        var flag = await flagRepository.GetFlag(flagId, token);
        if (flag is null || flag.ChallengeId != challengeId)
            return NotFound(new RequestResponse("Flag not found"));

        if (model.FlagValue is not null)
        {
            if (model.FlagValue.Length > Limits.MaxFlagLength)
                return BadRequest(new RequestResponse("Flag value is too long"));

            if (model.FlagValue != flag.FlagValue &&
                await flagRepository.ExistsByValue(challengeId, model.FlagValue, token))
                return BadRequest(new RequestResponse("This flag value already exists for this challenge"));

            flag.FlagValue = model.FlagValue;
        }

        if (model.AnswerType.HasValue)
            flag.AnswerType = model.AnswerType.Value;

        if (model.RawAnswer is not null)
        {
            flag.RawAnswer = model.RawAnswer;
        }
        else if (model.FlagValue is not null && flag.RawAnswer == flag.FlagValue)
        {
            // Keep RawAnswer in sync when it was mirroring FlagValue
            flag.RawAnswer = model.FlagValue;
        }

        if (model.IsVisible.HasValue)
            flag.IsVisible = model.IsVisible.Value;

        if (model.IsEnabled.HasValue)
            flag.IsEnabled = model.IsEnabled.Value;

        if (model.Level.HasValue && model.Level.Value > 0)
        {
            var newLevel = model.Level.Value;
            if (newLevel > 1)
            {
                var levelConfig = await levelConfigRepository.GetConfig(challengeId, newLevel, token);
                if (levelConfig is null)
                    return BadRequest(new RequestResponse($"Level {newLevel} config does not exist"));
            }
            flag.Level = newLevel;
        }

        if (model.TrackId.HasValue)
        {
            var track = await trackRepository.GetTrack(model.TrackId.Value, token);
            if (track is null || track.ChallengeId != challengeId)
                return BadRequest(new RequestResponse("Track does not belong to this challenge"));
            flag.TrackId = model.TrackId.Value;
        }

        if (string.IsNullOrWhiteSpace(flag.RawAnswer))
            flag.RawAnswer = flag.FlagValue;

        if (!TrySetNormalizedAnswerKey(flag, out var error))
            return BadRequest(error!);

        if (await HasNormalizedAnswerConflict(challengeId, flag.NormalizedAnswerKey, flag.Id, token))
            return BadRequest(new RequestResponse("This normalized answer already exists for this challenge"));

        if (model.SortOrder.HasValue)
            flag.SortOrder = model.SortOrder.Value;

        flag = await flagRepository.UpdateFlag(flag, token);
        await FlushScoreboardForChallenge(challengeId, token);
        return Ok(flag);
    }

    /// <summary>
    /// Delete a flag
    /// </summary>
    [RequireAdmin]
    [HttpDelete("challenges/{challengeId:int}/flags/{flagId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteFlag([FromRoute] int challengeId, [FromRoute] Guid flagId,
        CancellationToken token)
    {
        var flag = await flagRepository.GetFlag(flagId, token);
        if (flag is null || flag.ChallengeId != challengeId)
            return NotFound(new RequestResponse("Flag not found"));

        var hasSubmissions = await flagRepository.HasSubmissions(flagId, token);
        if (hasSubmissions)
            return Conflict(new RequestResponse("Cannot delete flag with existing submissions"));

        await flagRepository.DeleteFlag(flagId, token);
        await FlushScoreboardForChallenge(challengeId, token);
        return NoContent();
    }

    #endregion

    #region Penalty Configuration

    /// <summary>
    /// Get penalty configuration for a challenge
    /// </summary>
    [RequireAdmin]
    [HttpGet("challenges/{challengeId:int}/penalty")]
    [ProducesResponseType(typeof(ChallengePenalty), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPenalty([FromRoute] int challengeId, CancellationToken token)
    {
        var penalty = await penaltyRepository.GetOrCreatePenalty(challengeId, token);
        return Ok(penalty);
    }

    /// <summary>
    /// Update penalty configuration for a challenge
    /// </summary>
    [RequireAdmin]
    [HttpPut("challenges/{challengeId:int}/penalty")]
    [ProducesResponseType(typeof(ChallengePenalty), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdatePenalty([FromRoute] int challengeId,
        [FromBody] PenaltyUpdateModel model, CancellationToken token)
    {
        if (model.PenaltyScore < 0)
            return BadRequest(new RequestResponse("Penalty score cannot be negative"));

        var penalty = await penaltyRepository.GetOrCreatePenalty(challengeId, token);
        penalty.PenaltyScore = model.PenaltyScore;

        penalty = await penaltyRepository.UpdatePenalty(penalty, token);
        await FlushScoreboardForChallenge(challengeId, token);
        return Ok(penalty);
    }

    #endregion

    #region Competition Settings

    /// <summary>
    /// Get all competition settings
    /// </summary>
    [RequireAdmin]
    [HttpGet("settings")]
    [ProducesResponseType(typeof(Dictionary<string, string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSettings(CancellationToken token)
    {
        var settings = await settingsService.GetAll(token);
        return Ok(settings);
    }

    /// <summary>
    /// Update competition settings
    /// </summary>
    [RequireAdmin]
    [HttpPut("settings")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSettings([FromBody] Dictionary<string, JsonElement> settings,
        CancellationToken token)
    {
        foreach (var (key, element) in settings)
        {
            var value = UnwrapJsonElement(element);

            if (value is not null)
                await settingsService.Set(key, value, token);
        }

        return NoContent();
    }

    private static object? UnwrapJsonElement(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => TryParseNumber(element.GetString()),
        JsonValueKind.Number => element.TryGetInt32(out var intVal) ? intVal : element.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => null
    };

    private static object TryParseNumber(string? s)
    {
        if (s is null)
            return (object?)null;

        if (int.TryParse(s, out var intVal))
            return intVal;

        if (double.TryParse(s, out var doubleVal))
            return doubleVal;

        if (bool.TryParse(s, out var boolVal))
            return boolVal;

        return s;
    }

    #endregion

    #region BigScreen Events

    /// <summary>
    /// Get big screen events
    /// </summary>
    [RequireAdmin]
    [HttpGet("events")]
    [ProducesResponseType(typeof(BigScreenEvent[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEvents(
        [FromQuery] bool? unconsumedOnly = null,
        [FromQuery] int count = 100,
        CancellationToken token = default)
    {
        if (unconsumedOnly == true)
        {
            var events = await eventRepository.GetUnconsumedEvents(count, token);
            return Ok(events);
        }

        var allEvents = await eventRepository.GetAllEvents(count, token);
        return Ok(allEvents);
    }

    #endregion

    private static bool TrySetNormalizedAnswerKey(ChallengeFlag flag, out RequestResponse? error)
    {
        var normalizedKey = ScoringService.NormalizeHybridLineAnswer(flag.RawAnswer, flag.AnswerType);
        flag.NormalizedAnswerKey = normalizedKey;
        error = null;
        return true;
    }

    private async Task<bool> HasNormalizedAnswerConflict(int challengeId, string normalizedAnswerKey,
        Guid? currentFlagId, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(normalizedAnswerKey))
            return false;

        return await context.ChallengeFlags.AnyAsync(f =>
            f.ChallengeId == challengeId
            && f.NormalizedAnswerKey == normalizedAnswerKey
            && (!currentFlagId.HasValue || f.Id != currentFlagId.Value), token);
    }
}

#region Request Models

public class ChallengeFlagCreateModel
{
    public string FlagValue { get; set; } = string.Empty;
    public ChallengeFlagAnswerType AnswerType { get; set; } = ChallengeFlagAnswerType.Exact;
    public string? RawAnswer { get; set; }
    public bool IsVisible { get; set; } = true;
    public bool IsEnabled { get; set; } = true;
    public int Level { get; set; } = 1;
    public Guid? TrackId { get; set; }
    public int SortOrder { get; set; }
}

public class ChallengeFlagUpdateModel
{
    public string? FlagValue { get; set; }
    public ChallengeFlagAnswerType? AnswerType { get; set; }
    public string? RawAnswer { get; set; }
    public bool? IsVisible { get; set; }
    public bool? IsEnabled { get; set; }
    public int? Level { get; set; }
    public Guid? TrackId { get; set; }
    public int? SortOrder { get; set; }
}

public class PenaltyUpdateModel
{
    public int PenaltyScore { get; set; }
}

#endregion
