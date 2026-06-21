using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using GZCTF.Middlewares;
using GZCTF.Models;
using GZCTF.Repositories.Interface;
using Microsoft.AspNetCore.Mvc;

namespace GZCTF.Controllers;

[ApiController]
[Route("api/ext/challenges/{challengeId:int}/tracks")]
[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
public class TrackController(IChallengeTrackRepository trackRepository) : ControllerBase
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
        return CreatedAtAction(nameof(GetTrack), new { challengeId, trackId = track.Id }, track);
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
