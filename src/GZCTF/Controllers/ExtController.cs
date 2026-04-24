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
    CacheHelper cacheHelper) : ControllerBase
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

        if (model.Score < 1)
            return BadRequest(new RequestResponse("Score must be at least 1"));

        var exists = await flagRepository.ExistsByValue(challengeId, model.FlagValue, token);
        if (exists)
            return BadRequest(new RequestResponse("This flag value already exists for this challenge"));

        var flag = new ChallengeFlag
        {
            ChallengeId = challengeId,
            FlagValue = model.FlagValue,
            Score = model.Score,
            SortOrder = model.SortOrder
        };

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

        if (model.Score.HasValue)
        {
            if (model.Score.Value < 1)
                return BadRequest(new RequestResponse("Score must be at least 1"));
            flag.Score = model.Score.Value;
        }

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
    [ProducesResponseType(StatusCodes.Status200OK)]
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
}

#region Request Models

public class ChallengeFlagCreateModel
{
    public string FlagValue { get; set; } = string.Empty;
    public int Score { get; set; } = 100;
    public int SortOrder { get; set; }
}

public class ChallengeFlagUpdateModel
{
    public string? FlagValue { get; set; }
    public int? Score { get; set; }
    public int? SortOrder { get; set; }
}

public class PenaltyUpdateModel
{
    public int PenaltyScore { get; set; }
}

#endregion
