using System.Net.Mime;
using System.Security.Claims;
using GZCTF.Middlewares;
using GZCTF.Models.Request;
using GZCTF.Repositories.Interface;
using Microsoft.AspNetCore.Mvc;

namespace GZCTF.Controllers;

/// <summary>
/// Extended submission record APIs
/// </summary>
[ApiController]
[Route("api/ext/submissions")]
[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
public class ExtSubmissionController(
    ISubmissionRecordRepository recordRepository) : ControllerBase
{
    /// <summary>
    /// Get current user's submission records
    /// </summary>
    [RequireUser]
    [HttpGet("mine")]
    [ProducesResponseType(typeof(SubmissionRecordDto[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMySubmissions(
        [FromQuery] int? challengeId,
        [FromQuery] int? gameId,
        [FromQuery] int count = 100,
        [FromQuery] int skip = 0,
        CancellationToken token = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized(new RequestResponse("User not found"));

        var records = await recordRepository.GetByUser(Guid.Parse(userId), challengeId, gameId, count, skip, token);

        var result = records.Select(ToDto).ToArray();
        return Ok(result);
    }

    /// <summary>
    /// Get a specific user's submission records (admin only)
    /// </summary>
    [RequireAdmin]
    [HttpGet("user/{userId:guid}")]
    [ProducesResponseType(typeof(SubmissionRecordDto[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserSubmissions(
        [FromRoute] Guid userId,
        [FromQuery] int? challengeId,
        [FromQuery] int? gameId,
        [FromQuery] int count = 100,
        [FromQuery] int skip = 0,
        CancellationToken token = default)
    {
        var records = await recordRepository.GetByUser(userId, challengeId, gameId, count, skip, token);
        var result = records.Select(ToDto).ToArray();
        return Ok(result);
    }

    private static SubmissionRecordDto ToDto(SubmissionRecord record)
    {
        return new SubmissionRecordDto
        {
            Id = record.Id,
            SubmissionId = record.SubmissionId,
            ScoreChange = record.ScoreChange,
            IsPenalty = record.IsPenalty,
            CreatedAt = record.CreatedAt,
            ChallengeName = record.Submission?.ChallengeName ?? "Unknown",
            ChallengeId = record.Submission?.ChallengeId ?? 0,
            GameId = record.Submission?.GameId ?? 0,
            Answer = record.IsPenalty ? record.Submission?.Answer : null,
            MatchedFlagScore = record.MatchedFlag?.Score
        };
    }
}

public class SubmissionRecordDto
{
    public Guid Id { get; set; }
    public int SubmissionId { get; set; }
    public int ScoreChange { get; set; }
    public bool IsPenalty { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string ChallengeName { get; set; } = string.Empty;
    public int ChallengeId { get; set; }
    public int GameId { get; set; }
    public string? Answer { get; set; }
    public int? MatchedFlagScore { get; set; }
}
