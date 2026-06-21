using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using GZCTF.Middlewares;
using GZCTF.Models;
using GZCTF.Repositories.Interface;
using Microsoft.AspNetCore.Mvc;

namespace GZCTF.Controllers;

[ApiController]
[Route("api/ext/challenges/{challengeId:int}/levels")]
[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
public class LevelConfigController(IChallengeLevelConfigRepository levelConfigRepository) : ControllerBase
{
    [RequireAdmin]
    [HttpGet]
    [ProducesResponseType(typeof(ChallengeLevelConfig[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLevels([FromRoute] int challengeId, CancellationToken token)
    {
        var configs = await levelConfigRepository.GetConfigs(challengeId, token);
        return Ok(configs);
    }

    [RequireAdmin]
    [HttpPost]
    [ProducesResponseType(typeof(ChallengeLevelConfig), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateLevel([FromRoute] int challengeId,
        [FromBody] LevelConfigCreateModel model, CancellationToken token)
    {
        var existing = await levelConfigRepository.GetConfig(challengeId, model.Level, token);
        if (existing is not null)
            return Conflict(new RequestResponse($"Level {model.Level} already exists"));

        var config = new ChallengeLevelConfig
        {
            ChallengeId = challengeId,
            Level = model.Level,
            Title = model.Title,
            Score = model.Score,
            SubmissionLimit = model.SubmissionLimit ?? 0,
            PenaltyScore = model.PenaltyScore ?? 0,
            LockOnAnswers = model.LockOnAnswers ?? [],
            BanOnAnswers = model.BanOnAnswers ?? [],
            IsEnabled = model.IsEnabled ?? true,
            SortOrder = model.SortOrder ?? model.Level
        };

        config = await levelConfigRepository.CreateConfig(config, token);
        return CreatedAtAction(nameof(GetLevel), new { challengeId, levelId = config.Id }, config);
    }

    [RequireAdmin]
    [HttpGet("{levelId:int}")]
    [ProducesResponseType(typeof(ChallengeLevelConfig), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLevel([FromRoute] int challengeId, [FromRoute] int levelId, CancellationToken token)
    {
        var config = await levelConfigRepository.GetConfigById(levelId, token);
        if (config is null || config.ChallengeId != challengeId)
            return NotFound(new RequestResponse("Level config not found"));
        return Ok(config);
    }

    [RequireAdmin]
    [HttpPut("{levelId:int}")]
    [ProducesResponseType(typeof(ChallengeLevelConfig), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateLevel([FromRoute] int challengeId, [FromRoute] int levelId,
        [FromBody] LevelConfigUpdateModel model, CancellationToken token)
    {
        var config = await levelConfigRepository.GetConfigById(levelId, token);
        if (config is null || config.ChallengeId != challengeId)
            return NotFound(new RequestResponse("Level config not found"));

        if (model.Title is not null) config.Title = model.Title;
        if (model.Score.HasValue) config.Score = model.Score.Value;
        if (model.SubmissionLimit.HasValue) config.SubmissionLimit = model.SubmissionLimit.Value;
        if (model.PenaltyScore.HasValue) config.PenaltyScore = model.PenaltyScore.Value;
        if (model.LockOnAnswers is not null) config.LockOnAnswers = model.LockOnAnswers;
        if (model.BanOnAnswers is not null) config.BanOnAnswers = model.BanOnAnswers;
        if (model.IsEnabled.HasValue) config.IsEnabled = model.IsEnabled.Value;
        if (model.SortOrder.HasValue) config.SortOrder = model.SortOrder.Value;

        config = await levelConfigRepository.UpdateConfig(config, token);
        return Ok(config);
    }

    [RequireAdmin]
    [HttpDelete("{levelId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteLevel([FromRoute] int challengeId, [FromRoute] int levelId, CancellationToken token)
    {
        var config = await levelConfigRepository.GetConfigById(levelId, token);
        if (config is null || config.ChallengeId != challengeId)
            return NotFound(new RequestResponse("Level config not found"));

        // 查 Level 编号
        var level = config.Level;
        var hasFlags = await levelConfigRepository.HasFlags(challengeId, level, token);
        if (hasFlags)
            return Conflict(new RequestResponse("Cannot delete level with existing flags"));

        await levelConfigRepository.DeleteConfig(levelId, token);
        return NoContent();
    }
}

public class LevelConfigCreateModel
{
    [Range(1, int.MaxValue)]
    public int Level { get; set; }

    [MaxLength(64)]
    public string? Title { get; set; }

    [Range(0, int.MaxValue)]
    public int Score { get; set; }

    [Range(0, int.MaxValue)]
    public int? SubmissionLimit { get; set; }

    [Range(0, int.MaxValue)]
    public int? PenaltyScore { get; set; }

    public List<string>? LockOnAnswers { get; set; }
    public List<string>? BanOnAnswers { get; set; }
    public bool? IsEnabled { get; set; }

    [Range(0, int.MaxValue)]
    public int? SortOrder { get; set; }
}

public class LevelConfigUpdateModel
{
    [MaxLength(64)]
    public string? Title { get; set; }

    [Range(0, int.MaxValue)]
    public int? Score { get; set; }

    [Range(0, int.MaxValue)]
    public int? SubmissionLimit { get; set; }

    [Range(0, int.MaxValue)]
    public int? PenaltyScore { get; set; }

    public List<string>? LockOnAnswers { get; set; }
    public List<string>? BanOnAnswers { get; set; }
    public bool? IsEnabled { get; set; }

    [Range(0, int.MaxValue)]
    public int? SortOrder { get; set; }
}
