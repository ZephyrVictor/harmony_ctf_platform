using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Mime;
using System.Security.Claims;
using GZCTF.Middlewares;
using GZCTF.Models;
using GZCTF.Models.Internal;
using GZCTF.Models.Request.Admin;
using GZCTF.Models.Request.Game;
using GZCTF.Repositories.Interface;
using GZCTF.Services.Cache;
using GZCTF.Services.Config;
using GZCTF.Services.Scoring;
using GZCTF.Storage.Interface;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Npgsql;

namespace GZCTF.Controllers;

/// <summary>
/// Game related APIs
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
public class GameController(
    ILogger<GameController> logger,
    UserManager<UserInfo> userManager,
    CacheHelper cacheHelper,
    IBlobStorage storage,
    IConfigService configService,
    IBlobRepository blobService,
    IGameRepository gameRepository,
    ITeamRepository teamRepository,
    IDivisionRepository divisionRepository,
    IGameEventRepository eventRepository,
    IGameNoticeRepository noticeRepository,
    ICheatInfoRepository cheatInfoRepository,
    IContainerRepository containerRepository,
    IGameEventRepository gameEventRepository,
    ISubmissionRepository submissionRepository,
    ISubmissionRecordRepository submissionRecordRepository,
    IGameChallengeRepository challengeRepository,
    IGameInstanceRepository gameInstanceRepository,
    IParticipationRepository participationRepository,
    IOptionsSnapshot<ContainerPolicy> containerPolicy,
    IStringLocalizer<Program> localizer,
    AppDbContext dbContext,
    ScoringService scoringService,
    IChallengeTrackRepository trackRepository,
    IChallengeLevelConfigRepository levelConfigRepository,
    IChallengeLockStateRepository lockStateRepository,
    IChallengeTrackLevelBanStateRepository banStateRepository) : ControllerBase
{
    const string PositiveFlagUniqueIndexName = "IX_SubmissionRecords_ParticipationId_MatchedFlagId";

    /// <summary>
    /// Get the recent games
    /// </summary>
    /// <remarks>
    /// Retrieves recent game in three weeks
    /// </remarks>
    /// <param name="limit">Limit of the number of games</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved game information</response>
    [HttpGet("Recent")]
    [ProducesResponseType(typeof(BasicGameInfoModel[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> RecentGames(
        [FromQuery][Range(0, 50)] int limit,
        CancellationToken token)
    {
        (var games, var lastModified) = await gameRepository.GetRecentGames(token);
        var eTag = $"\"{lastModified.ToUnixTimeSeconds():X}-{limit}\"";
        if (ContextHelper.IsNotModified(Request, Response, eTag, lastModified))
            return StatusCode(StatusCodes.Status304NotModified);

        return Ok(limit > 0 ? games.Take(limit) : games);
    }

    /// <summary>
    /// Get games
    /// </summary>
    /// <remarks>
    /// Retrieves game information in specified range
    /// </remarks>
    /// <param name="count"></param>
    /// <param name="skip"></param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved game notices</response>
    /// <response code="400">Game not found</response>
    [HttpGet]
    [EnableRateLimiting(nameof(RateLimiter.LimitPolicy.Query))]
    [ResponseCache(VaryByQueryKeys = ["count", "skip"], Duration = 60)]
    [ProducesResponseType(typeof(ArrayResponse<BasicGameInfoModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Games([FromQuery][Range(0, 50)] int count = 10,
        [FromQuery] int skip = 0, CancellationToken token = default)
        => Ok(await gameRepository.GetGameInfo(count, skip, token));

    /// <summary>
    /// Get detailed game information
    /// </summary>
    /// <remarks>
    /// Retrieves detailed information about the game
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved game information</response>
    /// <response code="404">Game not found</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(DetailedGameInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Game(int id, CancellationToken token)
    {
        var gameInfo = await gameRepository.GetDetailedGameInfo(id, token);

        if (gameInfo is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        var count = await participationRepository.GetParticipationCount(id, token);

        Participation? part = null;
        if (await userManager.GetUserAsync(User) is { } user)
            part = await participationRepository.GetParticipation(user.Id, id, token);

        return Ok(gameInfo.WithParticipation(part, count));
    }

    /// <summary>
    /// Get check info for joining a game
    /// </summary>
    /// <param name="id"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    [RequireUser]
    [HttpGet("{id:int}/Check")]
    [ProducesResponseType(typeof(GameJoinCheckInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGameJoinCheckInfo(int id, CancellationToken token)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        var user = await userManager.GetUserAsync(User);

        return Ok(await gameRepository.GetCheckInfo(game, user!, token));
    }

    /// <summary>
    /// Join a game
    /// </summary>
    /// <remarks>
    /// Join a game; requires User permission
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="model"></param>
    /// <param name="token"></param>
    /// <response code="200">Successfully joined the game</response>
    /// <response code="403">Unauthorized operation or invalid operation</response>
    /// <response code="404">Game not found</response>
    [RequireUser]
    [HttpPost("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> JoinGame(int id, [FromBody] GameJoinModel model, CancellationToken token)
    {
        await using var transaction = await gameRepository.BeginTransactionAsync(token);
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        if (!game.PracticeMode && game.EndTimeUtc < DateTimeOffset.UtcNow)
            return BadRequest(
                new RequestResponse(localizer[nameof(Resources.Program.Game_Ended)], ErrorCodes.GameEnded));

        if (!model.CommitmentAccepted)
            return BadRequest(new RequestResponse(
                localizer[nameof(Resources.Program.Game_CommitmentRequired)],
                ErrorCodes.GameCommitmentRequired));

        var user = await userManager.GetUserAsync(User);
        var team = await teamRepository.GetTeamById(model.TeamId, token);

        if (team is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Team_NotFound)],
                StatusCodes.Status404NotFound));

        if (team.Members.All(u => u.Id != user!.Id))
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_NotMemberOfTeam)]));

        // =============== Validate division and permissions ===============

        var joinableDivisionIds = await divisionRepository.GetJoinableDivisionIds(id, token);

        Division? div = null;
        if (joinableDivisionIds is { Count: > 0 })
        {
            // We don't allow joining a game with joinable divisions without specifying a division
            if (model.DivisionId is null)
                return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_DivisionRequired)]));

            // Division must allow joining
            if (!joinableDivisionIds.Contains(model.DivisionId.Value))
                return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_InvalidDivision)]));

            div = await divisionRepository.GetDivision(id, model.DivisionId.Value, token);
            if (div is null)
                return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_InvalidDivision)]));

            // just redundant check, it takes little cost
            if (!div.DefaultPermissions.HasFlag(GamePermission.JoinGame))
                return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_InvalidDivision)]));
        }

        // =============== Validate invitation code ===============

        string? requiredInviteCode;
        if (div is not null)
            requiredInviteCode = string.IsNullOrEmpty(div.InviteCode) ? null : div.InviteCode;
        else
            requiredInviteCode = string.IsNullOrEmpty(game.InviteCode) ? null : game.InviteCode;

        if (requiredInviteCode is not null && requiredInviteCode != model.InviteCode)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_InvalidInvitationCode)]));

        // =============== Check and handle participation state ===============

        // Get existing participation for this team in this game
        var part = await participationRepository.GetParticipation(team, game, token);

        // Check if user is already in this game through a different team (exclude rejected participations)
        if (await participationRepository.CheckRepeatParticipation(user!, game, token))
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_InOtherTeam)]));

        // Always clean up rejected user participations in this game
        await participationRepository.RemoveUserParticipations(user!, game, token);

        if (part is null)
        {
            // Create new participation
            part = new() { Game = game, Team = team, Division = div, Token = gameRepository.GetToken(game, team) };
            participationRepository.Add(part);
        }
        else if (part.Status == ParticipationStatus.Rejected)
        {
            // Allow changing division when re-joining after rejection
            part.Division = div;
            part.Status = ParticipationStatus.Pending;
        }
        else if (part.DivisionId != model.DivisionId)
        {
            // If trying to change division, reject
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_InvalidDivision)]));
        }

        // =============== Verify team member count limit ===============

        if (game.TeamMemberCountLimit > 0 && part.Members.Count >= game.TeamMemberCountLimit)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_TeamMemberLimitExceeded)]));

        // =============== All checks passed, add user to the team ===============

        part.Members.Add(new(user!, game, team));

        await participationRepository.SaveAsync(token);

        var shouldAcceptWithoutReview = div is null
            ? game.AcceptWithoutReview
            : !div.DefaultPermissions.HasFlag(GamePermission.RequireReview);
        if (shouldAcceptWithoutReview)
            await participationRepository.UpdateParticipationStatus(part, ParticipationStatus.Accepted, token);

        await transaction.CommitAsync(token);
        await cacheHelper.FlushScoreboardCache(part.GameId, token);

        logger.Log(StaticLocalizer[nameof(Resources.Program.Game_JoinSucceeded), team.Name, game.Title], user,
            TaskStatus.Success);

        return Ok();
    }

    /// <summary>
    /// Leave a game
    /// </summary>
    /// <remarks>
    /// Leave a game; requires User permission
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully left the game</response>
    /// <response code="403">Unauthorized operation or invalid operation</response>
    /// <response code="404">Game not found</response>
    [RequireUser]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> LeaveGame(int id, CancellationToken token)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        var user = await userManager.GetUserAsync(User);

        var part = await participationRepository.GetParticipation(user!.Id, game.Id, token);

        if (part is null || part.Members.All(u => u.UserId != user.Id))
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_CannotLeaveWithoutJoin)]));

        if (part.Status != ParticipationStatus.Pending && part.Status != ParticipationStatus.Rejected)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_CannotLeaveAfterApproval)]));

        // FIXME: After approval, new users can be added, but cannot exit?
        part.Members.RemoveWhere(u => u.UserId == user.Id);

        if (part.Members.Count == 0)
            await participationRepository.RemoveParticipation(part, true, token);
        else
            await participationRepository.SaveAsync(token);

        return Ok();
    }

    /// <summary>
    /// Get the scoreboard
    /// </summary>
    /// <remarks>
    /// Retrieves the scoreboard data
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved game information</response>
    /// <response code="400">Game not found</response>
    [HttpGet("{id:int}/Scoreboard")]
    [ProducesResponseType(typeof(ScoreboardModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Scoreboard([FromRoute] int id, CancellationToken token)
    {
        var scoreboard = await gameRepository.TryGetScoreboard(id, token);
        string eTag;
        if (scoreboard is not null)
        {
            eTag = GameETag(id, scoreboard.UpdateTimeUtc);
            if (ContextHelper.IsNotModified(Request, Response, eTag, scoreboard.UpdateTimeUtc))
                return StatusCode(StatusCodes.Status304NotModified);

            return Ok(scoreboard);
        }

        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        if (DateTimeOffset.UtcNow < game.StartTimeUtc)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_NotStarted)]));

        scoreboard = await gameRepository.GetScoreboard(game, token);
        var lastModified = scoreboard.UpdateTimeUtc;
        eTag = GameETag(game.Id, lastModified);
        ContextHelper.SetCacheHeaders(Response, eTag, lastModified);

        return Ok(scoreboard);
    }

    /// <summary>
    /// Get game notices
    /// </summary>
    /// <remarks>
    /// Retrieves game notice data
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="count"></param>
    /// <param name="skip"></param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved game notices</response>
    /// <response code="400">Game not found</response>
    [HttpGet("{id:int}/Notices")]
    [ProducesResponseType(typeof(GameNotice[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Notices([FromRoute] int id, [FromQuery][Range(0, 100)] int count = 100,
        [FromQuery][Range(0, 300)] int skip = 0, CancellationToken token = default)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        if (DateTimeOffset.UtcNow < game.StartTimeUtc)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_NotStarted)]));

        (var data, var lastModified) = await noticeRepository.GetLatestNotices(game.Id, token);
        var eTag = $"\"{game.Id}-{lastModified.ToUnixTimeSeconds():X}-{skip}-{count}\"";
        if (ContextHelper.IsNotModified(Request, Response, eTag, lastModified))
            return StatusCode(StatusCodes.Status304NotModified);
        return Ok(data.Skip(skip).Take(count));
    }

    /// <summary>
    /// Get game events
    /// </summary>
    /// <remarks>
    /// Retrieves game event data; requires Monitor permission
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="count"></param>
    /// <param name="hideContainer">Hide container events</param>
    /// <param name="skip"></param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved game events</response>
    /// <response code="400">Game not found</response>
    [RequireMonitor]
    [HttpGet("{id:int}/Events")]
    [ProducesResponseType(typeof(GameEvent[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Events([FromRoute] int id, [FromQuery] bool hideContainer = false,
        [FromQuery][Range(0, 100)] int count = 100, [FromQuery] int skip = 0, CancellationToken token = default)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        if (DateTimeOffset.UtcNow < game.StartTimeUtc)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_NotStarted)]));

        return Ok(await eventRepository.GetEvents(game.Id, hideContainer, count, skip, token));
    }

    /// <summary>
    /// Get game submissions
    /// </summary>
    /// <remarks>
    /// Retrieves game submission data; requires Monitor permission
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="type">Submission type</param>
    /// <param name="count"></param>
    /// <param name="skip"></param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved game submissions</response>
    /// <response code="400">Game not found</response>
    [RequireMonitor]
    [HttpGet("{id:int}/Submissions")]
    [ProducesResponseType(typeof(Submission[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Submissions([FromRoute] int id, [FromQuery] AnswerResult? type = null,
        [FromQuery][Range(0, 100)] int count = 100, [FromQuery] int skip = 0, CancellationToken token = default)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        if (DateTimeOffset.UtcNow < game.StartTimeUtc)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_NotStarted)]));

        return Ok(await submissionRepository.GetSubmissions(game, type, count, skip, token));
    }

    /// <summary>
    /// Get game cheat information
    /// </summary>
    /// <remarks>
    /// Retrieves game cheat data; requires Monitor permission
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved game cheat data</response>
    /// <response code="400">Game not found</response>
    [RequireMonitor]
    [HttpGet("{id:int}/CheatInfo")]
    [ProducesResponseType(typeof(CheatInfoModel[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheatInfo([FromRoute] int id, CancellationToken token = default)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound));

        if (DateTimeOffset.UtcNow < game.StartTimeUtc)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_NotStarted)]));

        return Ok((await cheatInfoRepository.GetCheatInfoByGameId(game.Id, token))
            .Select(CheatInfoModel.FromCheatInfo));
    }

    /// <summary>
    /// Get challenges with traffic capturing enabled
    /// </summary>
    /// <remarks>
    /// Retrieves challenges with traffic capturing enabled; requires Monitor permission
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved challenge list</response>
    /// <response code="404">Capture information not found</response>
    [RequireMonitor]
    [HttpGet("Games/{id:int}/Captures")]
    [ProducesResponseType(typeof(ChallengeTrafficModel[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetChallengesWithTrafficCapturing([FromRoute] int id, CancellationToken token)
    {
        var challenges = await challengeRepository.GetChallengesWithTrafficCapturing(id, token);

        var results = await Task.WhenAll(
            challenges.Select(c => ChallengeTrafficModel.FromChallengeAsync(c, storage, token))
        );

        return Ok(results);
    }

    /// <summary>
    /// Get team captures in a challenge
    /// </summary>
    /// <remarks>
    /// Retrieves the list of captured teams for a game challenge; requires Monitor permission
    /// </remarks>
    /// <param name="challengeId">Challenge ID</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved file list</response>
    /// <response code="404">Capture information not found</response>
    [RequireMonitor]
    [HttpGet("Captures/{challengeId:int}")]
    [ProducesResponseType(typeof(TeamTrafficModel[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetChallengeTraffic([FromRoute] int challengeId, CancellationToken token)
    {
        var path = StoragePath.Combine(PathHelper.Capture, $"{challengeId}");

        var entries = await storage.ListAsync(path, cancellationToken: token);
        var participationIds = entries.Select(e => int.TryParse(e.Name, out var id) ? id : -1)
            .Where(id => id > 0).ToArray();

        if (participationIds.Length == 0)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_CaptureNotFound)],
                StatusCodes.Status404NotFound));

        var participation = await participationRepository.GetParticipationsByIds(participationIds, token);

        var results = await Task.WhenAll(
            participation.Select(p => TeamTrafficModel.FromParticipationAsync(p, challengeId, storage, token))
        );

        return Ok(results);
    }

    /// <summary>
    /// Get traffic files
    /// </summary>
    /// <remarks>
    /// Retrieves traffic packet files for a team and challenge; requires Monitor permission
    /// </remarks>
    /// <param name="challengeId">Challenge ID</param>
    /// <param name="partId">Team participation ID</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved file list</response>
    /// <response code="404">Capture information not found</response>
    [RequireMonitor]
    [HttpGet("Captures/{challengeId:int}/{partId:int}")]
    [ProducesResponseType(typeof(FileRecord[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTeamTraffic([FromRoute] int challengeId, [FromRoute] int partId,
        CancellationToken token)
    {
        var path = StoragePath.Combine(PathHelper.Capture, $"{challengeId}", $"{partId}");

        var blobs = await storage.ListAsync(path, cancellationToken: token);

        var results = blobs.Select(blob => new FileRecord
        {
            FileName = blob.Name,
            Size = blob.Size ?? 0,
            UpdateTime = blob.LastModificationTime ?? DateTimeOffset.MinValue
        }).ToArray();

        return Ok(results);
    }

    /// <summary>
    /// Download all traffic files
    /// </summary>
    /// <remarks>
    /// Downloads all traffic packet files for a team and challenge; requires Monitor permission
    /// </remarks>
    /// <param name="challengeId">Challenge ID</param>
    /// <param name="partId">Team participation ID</param>
    /// <param name="token">Token</param>
    /// <response code="200">Successfully retrieved files</response>
    /// <response code="404">Capture information not found</response>
    [RequireMonitor]
    [HttpGet("Captures/{challengeId:int}/{partId:int}/All")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetAllTeamTraffic([FromRoute] int challengeId, [FromRoute] int partId,
        CancellationToken token)
    {
        var path = StoragePath.Combine(PathHelper.Capture, $"{challengeId}", $"{partId}");

        var filename = $"Capture-{challengeId}-{partId}-{DateTimeOffset.UtcNow:yyyyMMdd-HH.mm.ssZ}";

        return new TarDirectoryResult(storage, path, filename, token);
    }

    /// <summary>
    /// Deletes all traffic files
    /// </summary>
    /// <remarks>
    /// Deletes a team's traffic packet files for a challenge; requires Monitor permission
    /// </remarks>
    /// <param name="challengeId">Challenge ID</param>
    /// <param name="partId">Team participation ID</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully deleted files</response>
    /// <response code="404">Capture information not found</response>
    [RequireMonitor]
    [HttpDelete("Captures/{challengeId:int}/{partId:int}/All")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteAllTeamTraffic([FromRoute] int challengeId, [FromRoute] int partId,
        CancellationToken token)
    {
        try
        {
            var path = StoragePath.Combine(PathHelper.Capture, $"{challengeId}", $"{partId}");

            await storage.DeleteAsync(path, token);

            return Ok();
        }
        catch (Exception e)
        {
            return BadRequest(new RequestResponse(e.Message));
        }
    }

    /// <summary>
    /// Get a traffic file
    /// </summary>
    /// <remarks>
    /// Retrieves a traffic packet file; requires Monitor permission
    /// </remarks>
    /// <param name="challengeId">Challenge ID</param>
    /// <param name="partId">Team participation ID</param>
    /// <param name="filename">Traffic packet filename</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved file</response>
    /// <response code="404">Capture information not found</response>
    [RequireMonitor]
    [HttpGet("Captures/{challengeId:int}/{partId:int}/{filename}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTeamTraffic([FromRoute] int challengeId, [FromRoute] int partId,
        [FromRoute] string filename, CancellationToken token)
    {
        try
        {
            var path = StoragePath.Combine(PathHelper.Capture, $"{challengeId}", $"{partId}", filename);

            if (!await storage.ExistsAsync(path, token))
                return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_CaptureNotFound)]));

            var stream = await storage.OpenReadAsync(path, token);

            return File(stream, MediaTypeNames.Application.Octet, filename);
        }
        catch
        {
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_CaptureNotFound)]));
        }
    }

    /// <summary>
    /// Deletes a traffic file
    /// </summary>
    /// <remarks>
    /// Deletes a traffic packet file; requires Monitor permission
    /// </remarks>
    /// <param name="challengeId">Challenge ID</param>
    /// <param name="partId">Team participation ID</param>
    /// <param name="filename">Traffic packet filename</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully deleted file</response>
    /// <response code="404">Capture information not found</response>
    [RequireMonitor]
    [HttpDelete("Captures/{challengeId:int}/{partId:int}/{filename}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTeamTraffic([FromRoute] int challengeId, [FromRoute] int partId,
        [FromRoute] string filename, CancellationToken token)
    {
        try
        {
            var path = StoragePath.Combine(PathHelper.Capture, $"{challengeId}", $"{partId}", filename);

            if (!await storage.ExistsAsync(path, token))
                return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_CaptureNotFound)]));

            await storage.DeleteAsync(path, token);

            return Ok();
        }
        catch
        {
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_CaptureNotFound)]));
        }
    }

    /// <summary>
    /// Get team details in a game
    /// </summary>
    /// <remarks>
    /// Retrieves all challenges of the game; requires User permission and active team participation
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved game challenge information</response>
    /// <response code="400">Invalid operation</response>
    /// <response code="404">Game not found</response>
    [RequireUser]
    [HttpGet("{id:int}/Details")]
    [ProducesResponseType(typeof(GameDetailModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChallengesWithTeamInfo([FromRoute] int id, CancellationToken token)
    {
        var gameClosed = await gameRepository.IsGameClosed(id, token);
        if (gameClosed)
            return BadRequest(
                new RequestResponse(localizer[nameof(Resources.Program.Game_Ended)], ErrorCodes.GameEnded));

        var scoreboard = await gameRepository.TryGetScoreboard(id, token);
        string eTag;
        if (scoreboard is not null)
        {
            eTag = GameETag(id, scoreboard.UpdateTimeUtc);
            if (ContextHelper.IsNotModified(Request, Response, eTag, scoreboard.UpdateTimeUtc, true))
                return StatusCode(StatusCodes.Status304NotModified);
        }

        var context = await GetContextInfo(id, token: token);

        if (context.Result is not null)
            return context.Result;

        scoreboard ??= await gameRepository.GetScoreboard(context.Game!, token);
        var lastModified = scoreboard.UpdateTimeUtc;
        eTag = GameETag(context.Game!.Id, lastModified);
        ContextHelper.SetCacheHeaders(Response, eTag, lastModified, true);

        var challenges = scoreboard.Challenges;
        if (context.Participation!.DivisionId is { } divId &&
            scoreboard.Divisions.TryGetValue(divId, out var division))
        {
            // filter out challenges is can be viewed by division permission
            challenges = FilterChallengesByPermission(scoreboard.Challenges, division);
        }

        var boardItem = scoreboard.Items.TryGetValue(context.Participation!.TeamId, out var item)
            ? item
            : new()
            {
                Avatar = context.Participation!.Team.AvatarUrl,
                Rank = 0,
                Name = context.Participation!.Team.Name,
                Id = context.Participation!.TeamId
            };

        return Ok(new GameDetailModel
        {
            ScoreboardItem = boardItem,
            TeamToken = context.Participation!.Token,
            Challenges = challenges,
            ChallengeCount = challenges.Count,
            WriteupRequired = context.Game!.WriteupRequired,
            WriteupDeadline = context.Game!.WriteupDeadline,
            ZeroDayInfoMarkdown = context.Game!.ZeroDayInfoMarkdown,
            ZeroDayBaseScore = context.Game!.ZeroDayBaseScore
        });
    }

    /// <summary>
    /// Get all game participations
    /// </summary>
    /// <remarks>
    /// Retrieves all participation information of the game; requires Admin permission
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved game participation information</response>
    /// <response code="400">Invalid operation</response>
    /// <response code="404">Game not found</response>
    [RequireAdmin]
    [HttpGet("{id:int}/Participations")]
    [ProducesResponseType(typeof(ParticipationInfoModel[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Participations([FromRoute] int id, CancellationToken token = default)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)]));

        return Ok((await participationRepository.GetParticipations(game, token))
            .Select(ParticipationInfoModel.FromParticipation));
    }

    /// <summary>
    /// Downloads the scoreboard
    /// </summary>
    /// <remarks>
    /// Downloads the game scoreboard; requires Monitor permission
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="excelHelper"></param>
    /// <param name="token"></param>
    /// <response code="200">Successfully downloaded game scoreboard</response>
    /// <response code="400">Invalid operation</response>
    /// <response code="404">Game not found</response>
    [RequireMonitor]
    [HttpGet("{id:int}/ScoreboardSheet")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public async Task<IActionResult> ScoreboardSheet([FromRoute] int id, [FromServices] ExcelHelper excelHelper,
        CancellationToken token = default)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)]));

        if (DateTimeOffset.UtcNow < game.StartTimeUtc)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_NotStarted)]));

        try
        {
            var scoreboard = await gameRepository.GetScoreboardWithMembers(game, token);
            var stream = excelHelper.GetScoreboardExcel(scoreboard);
            stream.Seek(0, SeekOrigin.Begin);

            return File(stream,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{game.Title}-Scoreboard-{DateTimeOffset.Now:yyyyMMdd-HH.mm.ssZ}.xlsx");
        }
        catch (Exception ex)
        {
            logger.SystemLog(StaticLocalizer[nameof(Resources.Program.Game_ScoreboardDownloadFailed)],
                TaskStatus.Failed, LogLevel.Error);
            logger.LogErrorMessage(ex, ex.Message);
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_ScoreboardDownloadFailed)]));
        }
    }

    /// <summary>
    /// Downloads all submissions
    /// </summary>
    /// <remarks>
    /// Downloads all submissions of the game; requires Monitor permission
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="excelHelper"></param>
    /// <param name="token"></param>
    /// <response code="200">Successfully downloaded all game submissions</response>
    /// <response code="400">Invalid operation</response>
    /// <response code="404">Game not found</response>
    [RequireMonitor]
    [HttpGet("{id:int}/SubmissionSheet")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public async Task<IActionResult> SubmissionSheet([FromRoute] int id, [FromServices] ExcelHelper excelHelper,
        CancellationToken token = default)
    {
        var game = await gameRepository.GetGameById(id, token);

        if (game is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)]));

        if (DateTimeOffset.UtcNow < game.StartTimeUtc)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_NotStarted)]));

        var submissions = await submissionRepository.GetSubmissions(game, count: 0, token: token);

        var stream = excelHelper.GetSubmissionExcel(submissions);
        stream.Seek(0, SeekOrigin.Begin);

        return File(stream,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"{game.Title}_Submissions_{DateTimeOffset.Now:yyyyMMddHHmmss}.xlsx");
    }

    /// <summary>
    /// Get challenge information
    /// </summary>
    /// <remarks>
    /// Retrieves challenge information; requires User permission and active team participation
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="challengeId">Challenge ID</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved game challenge information</response>
    /// <response code="400">Invalid operation</response>
    /// <response code="404">Game not found</response>
    [RequireUser]
    [HttpGet("{id:int}/Challenges/{challengeId:int}")]
    [ProducesResponseType(typeof(ChallengeDetailModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetChallenge([FromRoute] int id, [FromRoute] int challengeId,
        CancellationToken token)
    {
        if (id <= 0 || challengeId <= 0)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Challenge_NotFound)],
                StatusCodes.Status404NotFound));

        var context = await GetContextInfo(id, token: token);

        if (context.Result is not null)
            return context.Result;

        var permission = await divisionRepository.GetPermission(context.Participation?.DivisionId, challengeId, token);

        if (!permission.HasFlag(GamePermission.ViewChallenge))
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Challenge_NotFound)],
                StatusCodes.Status404NotFound));

        var instance = await gameInstanceRepository.GetInstance(context.Participation!, challengeId, token);

        if (instance is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_ChallengeNotFound)],
                StatusCodes.Status404NotFound));

        var scoreboard = await gameRepository.GetScoreboard(context.Game!, token);
        var scoreboardChallenge =
            scoreboard.ChallengeMap.TryGetValue(challengeId, out var challenge) ? challenge : null;

        var attempts = await submissionRepository.CountSubmissions(context.Participation!.Id, challengeId, token);

        ISet<Guid>? solvedFlagIds = null;
        solvedFlagIds = (await submissionRecordRepository.GetSolvedFlagIds(
            challengeId, context.Participation!.Id, token)).ToHashSet();

        var result = ChallengeDetailModel.FromInstance(instance, attempts, scoreboardChallenge, solvedFlagIds);

        // 填充多级状态
        var tracks = await trackRepository.GetTracks(challengeId, token);
        var levelConfigs = await levelConfigRepository.GetConfigs(challengeId, token);
        var bans = await banStateRepository.GetBans(context.Participation!.Id, challengeId, token);
        var activeLock = await lockStateRepository.GetActiveLock(context.Participation!.Id, challengeId, token);
        var solvedTrackLevels = await submissionRecordRepository.GetSolvedTrackLevels(challengeId, context.Participation!.Id, token);
        var solvedSet = solvedTrackLevels.Select(t => (t.TrackId, t.Level)).ToHashSet();
        // 拉取每个 (TrackId, Level) 的解决时间，用于填充 SolvedAt
        var solvedTimeMap = solvedTrackLevels
            .ToDictionary(t => (t.TrackId, t.Level), t => t.SolvedAt);

        var trackStatuses = new List<TrackStatusModel>();

        // ActiveTrackId：选手在该题目下最近一次有提交记录的 TrackId
        var activeTrackId = await submissionRecordRepository.GetLatestSubmissionTrackId(
            challengeId, context.Participation!.Id, token);
        result.ActiveTrackId = activeTrackId;

        // 计算 CurrentLevel：基于 ActiveTrackId
        // - ActiveTrackId == null → 1
        // - 该 track 全解 → 1（准备开新漏洞）
        // - 否则 → 该 track 下一个未解且未封禁的 level
        int currentLevel = 1;
        if (activeTrackId is not null)
        {
            var activeTrack = tracks.FirstOrDefault(t => t.Id == activeTrackId.Value);
            if (activeTrack is not null)
            {
                var activeLevels = levelConfigs.Where(c => c.IsEnabled).OrderBy(c => c.Level).ToList();
                var nextUnsolved = activeLevels.FirstOrDefault(lc =>
                    !solvedSet.Contains((activeTrack.Id, lc.Level))
                    && !bans.Any(b => b.TrackId == activeTrack.Id && b.Level == lc.Level));
                currentLevel = nextUnsolved?.Level ?? 1;
            }
        }

        foreach (var track in tracks.Where(t => t.IsEnabled))
        {
            var trackStatus = new TrackStatusModel
            {
                TrackId = track.Id,
                Title = track.Title,
                IsEnabled = track.IsEnabled,
                Levels = new List<TrackLevelStatusModel>()
            };

            foreach (var lc in levelConfigs.Where(c => c.IsEnabled).OrderBy(c => c.Level))
            {
                var isSolved = solvedSet.Contains((track.Id, lc.Level));
                var isBanned = bans.Any(b => b.TrackId == track.Id && b.Level == lc.Level);
                var prevSolved = lc.Level == 1 || solvedSet.Contains((track.Id, lc.Level - 1));

                string status = activeLock is not null && !isSolved ? "Locked"
                    : isSolved ? "Solved"
                    : isBanned ? "Banned"
                    : !prevSolved ? "Locked"
                    : "Unsolved";

                var attemptsUsed = await submissionRecordRepository.CountSubmissionsByTrackLevel(
                    challengeId, context.Participation!.Id, track.Id, lc.Level, token);

                DateTimeOffset? solvedAt = isSolved && solvedTimeMap.TryGetValue((track.Id, lc.Level), out var t)
                    ? t : null;

                trackStatus.Levels.Add(new TrackLevelStatusModel
                {
                    Level = lc.Level,
                    Title = lc.Title,
                    Status = status,
                    AttemptsUsed = attemptsUsed,
                    SubmissionLimit = lc.SubmissionLimit > 0 ? lc.SubmissionLimit : instance.Challenge.SubmissionLimit,
                    UnlocksAt = activeLock?.UnlocksAt,
                    SolvedAt = solvedAt
                });
            }

            trackStatuses.Add(trackStatus);
        }

        result.Tracks = trackStatuses;
        var activeBan = bans.FirstOrDefault(b => activeTrackId.HasValue && b.TrackId == activeTrackId.Value);

        result.ChallengeLock = new ChallengeLockStatusModel
        {
            IsLocked = activeLock is not null,
            UnlocksAt = activeLock?.UnlocksAt,
            Reason = activeLock?.TriggerReason,
            TriggerAnswer = activeLock?.TriggerAnswer,
            BannedTrackId = activeBan?.TrackId,
            BannedLevel = activeBan?.Level,
            BanTriggerAnswer = activeBan?.BanAnswer,
        };
        result.CurrentLevel = currentLevel;

        return Ok(result);
    }

    /// <summary>
    /// Submits a flag
    /// </summary>
    /// <remarks>
    /// Submits a flag; requires User permission and active team participation
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="challengeId">Challenge ID</param>
    /// <param name="model">Flag submission</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved game challenge information</response>
    /// <response code="400">Invalid operation</response>
    /// <response code="404">Game not found</response>
    [RequireUser]
    [HttpPost("{id:int}/Challenges/{challengeId:int}")]
    [EnableRateLimiting(nameof(RateLimiter.LimitPolicy.Submit))]
    [ProducesResponseType(typeof(FlagSubmitResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Submit([FromRoute] int id, [FromRoute] int challengeId,
        [FromBody] FlagSubmitModel model, CancellationToken token)
    {
        var submitTime = DateTimeOffset.UtcNow;
        var answer = configService.DecryptApiData(model.Flag);
        if (string.IsNullOrWhiteSpace(answer))
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Model_FlagRequired)]));

        if (answer.Length > Limits.MaxFlagLength)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Model_FlagTooLong)]));

        var context = await GetContextInfo(id, token: token);

        if (context.Result is not null)
            return context.Result;

        const int maxRetries = 3;
        for (var retry = 0; retry < maxRetries; retry++)
        {
            await using var transaction = await gameInstanceRepository.BeginTransactionAsync(token);

            var instance =
                await gameInstanceRepository.GetInstanceForSubmission(context.Participation!, challengeId, token);

            if (instance is null)
                return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_ChallengeNotFound)],
                    StatusCodes.Status404NotFound));

            // Check if submission exceeds challenge deadline (only reject in non-practice mode)
            var hasExceededDeadline = instance.Challenge.DeadlineUtc is { } deadline && submitTime > deadline;
            if (hasExceededDeadline && !context.Game!.PracticeMode)
                return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Challenge_DeadlinePassed)]));

            var permission =
                await divisionRepository.GetPermission(context.Participation?.DivisionId, challengeId, token);

            if (!permission.HasFlag(GamePermission.ViewChallenge | GamePermission.SubmitFlags))
                return BadRequest(
                    new RequestResponse(localizer[nameof(Resources.Program.Challenge_SubmissionNoPermission)]));

            var currentAttempts =
                await submissionRepository.CountSubmissions(context.Participation!.Id, challengeId, token);

            if (instance.Challenge.SubmissionLimit > 0 && currentAttempts >= instance.Challenge.SubmissionLimit)
            {
                return BadRequest(
                    new RequestResponse(localizer[nameof(Resources.Program.Challenge_SubmissionLimitExceeded)]));
            }

            ScoreResult? scoreResult = null;
            try
            {
                // Process submission through scoring service
                scoreResult = await scoringService.ProcessSubmission(
                    instance.Challenge, context.Participation!, answer,
                    model.TrackId, model.Level, token);
            }
            catch (InvalidFlagSubmissionException ex)
            {
                await transaction.RollbackAsync(token);
                return BadRequest(new RequestResponse(ex.Message));
            }

            var result = scoreResult!;
            Submission submission = new()
            {
                Game = context.Game!,
                User = context.User!,
                GameChallenge = instance.Challenge,
                Team = context.Participation!.Team,
                Participation = context.Participation!,
                Status = result.Status,
                SubmitTimeUtc = submitTime,
                Answer = answer
            };

            try
            {
                submission = await submissionRepository.AddSubmission(submission, token);

                await scoringService.CreateRecord(submission, result, token);

                // Create FirstSolves entry with the actual submission ID
                if (result.SubmissionType != SubmissionType.Unaccepted)
                {
                    await scoringService.CreateFirstSolve(
                        submission.ParticipationId, submission.ChallengeId, submission.Id, token);
                }

                await transaction.CommitAsync(token);

                // Send via SignalR for real-time updates
                await submissionRepository.SendSubmission(submission);

                // Create blood bonus notice
                if (context.Game!.EndTimeUtc > DateTimeOffset.UtcNow
                    && result.SubmissionType != SubmissionType.Unaccepted
                    && result.SubmissionType != SubmissionType.Normal)
                {
                    await noticeRepository.AddNotice(
                        GameNotice.FromSubmission(submission, result.SubmissionType, localizer), token);
                }

                // Flush scoreboard cache so the new SubmissionRecord is reflected
                await cacheHelper.FlushScoreboardCache(context.Game!.Id, token);

                // D6+D7+D8: 响应携带多级 flag 状态信息
                // SolvedCount：按 Track 计数（L1+L2+L3 全解才算 1 个）
                var solvedTrackLevels = await submissionRecordRepository.GetSolvedTrackLevels(
                    challengeId, context.Participation!.Id, token);
                var solvedTrackIds = solvedTrackLevels.Select(t => t.TrackId).Distinct().ToHashSet();
                var allLevels = (await levelConfigRepository.GetConfigs(challengeId, token))
                    .Where(c => c.IsEnabled).Select(c => c.Level).Distinct().ToList();
                var solvedCount = allLevels.Count > 0
                    ? solvedTrackIds.Count(tid =>
                        allLevels.All(lvl => solvedTrackLevels.Any(s => s.TrackId == tid && s.Level == lvl)))
                    : 0;

                // IsTrackComplete：当前 track 的最高 Level 是否已解
                var isTrackComplete = false;
                if (result.TrackId is not null && allLevels.Count > 0)
                {
                    var maxLevel = allLevels.Max();
                    isTrackComplete = solvedTrackLevels.Any(s =>
                        s.TrackId == result.TrackId.Value && s.Level == maxLevel);
                }

                // ActiveTrackId：当前活跃 TrackId（= TrackId 字段的显式别名）
                var activeTrackId = result.TrackId;

                // TriggerAnswer：触发锁/封禁的错误答案
                string? triggerAnswer = null;
                if (result.ChallengeLocked || result.BannedTrackId is not null)
                {
                    triggerAnswer = answer;
                }

                return Ok(new FlagSubmitResponse
                {
                    Id = submission.Id,
                    Message = result.Message,
                    TrackId = result.TrackId,
                    Level = result.Level,
                    SolvedCount = solvedCount,
                    ActiveTrackId = activeTrackId,
                    IsTrackComplete = isTrackComplete,
                    ChallengeLocked = result.ChallengeLocked,
                    UnlocksAt = result.UnlocksAt,
                    BannedTrackId = result.BannedTrackId,
                    BannedLevel = result.BannedLevel,
                    TriggerAnswer = triggerAnswer
                });
            }
            catch (DbUpdateConcurrencyException) when (retry < maxRetries - 1)
            {
                await transaction.RollbackAsync(token);
                await Task.Delay((retry + 1) * 100, token);
            }
            catch (DbUpdateException ex) when (IsPositiveFlagDuplicateConstraintViolation(scoreResult, ex))
            {
                await transaction.RollbackAsync(token);
                dbContext.ChangeTracker.Clear();
                logger.LogWarning(ex, "Treating duplicate positive flag submission as idempotent for challenge {ChallengeId}",
                    challengeId);

                var existingRecord = await dbContext.SubmissionRecords
                    .AsNoTracking()
                    .Where(r => r.MatchedFlagId == scoreResult!.MatchedFlagId
                                && r.ScoreChange > 0
                                && r.ParticipationId == context.Participation!.Id)
                    .OrderBy(r => r.CreatedAt)
                    .Select(r => new { r.SubmissionId, r.TrackId, r.Level })
                    .FirstOrDefaultAsync(token);

                if (existingRecord is not null)
                {
                    // 重复提交：仍然计算 SolvedCount / IsTrackComplete / ActiveTrackId 供前端更新
                    var dupSolvedTrackLevels = await submissionRecordRepository.GetSolvedTrackLevels(
                        challengeId, context.Participation!.Id, token);
                    var dupSolvedTrackIds = dupSolvedTrackLevels.Select(t => t.TrackId).Distinct().ToHashSet();
                    var dupAllLevels = (await levelConfigRepository.GetConfigs(challengeId, token))
                        .Where(c => c.IsEnabled).Select(c => c.Level).Distinct().ToList();
                    var dupSolvedCount = dupAllLevels.Count > 0
                        ? dupSolvedTrackIds.Count(tid =>
                            dupAllLevels.All(lvl => dupSolvedTrackLevels.Any(s => s.TrackId == tid && s.Level == lvl)))
                        : 0;
                    var dupIsTrackComplete = false;
                    if (existingRecord.TrackId is not null && dupAllLevels.Count > 0)
                    {
                        var dupMaxLevel = dupAllLevels.Max();
                        dupIsTrackComplete = dupSolvedTrackLevels.Any(s =>
                            s.TrackId == existingRecord.TrackId.Value && s.Level == dupMaxLevel);
                    }

                    return Ok(new FlagSubmitResponse
                    {
                        Id = existingRecord.SubmissionId,
                        Message = "This flag has already been submitted",
                        TrackId = existingRecord.TrackId,
                        Level = existingRecord.Level,
                        SolvedCount = dupSolvedCount,
                        ActiveTrackId = existingRecord.TrackId,
                        IsTrackComplete = dupIsTrackComplete
                    });
                }

                logger.LogError(ex,
                    "Positive flag unique constraint violation found no existing submission for challenge {ChallengeId}",
                    challengeId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new RequestResponse(localizer[nameof(Resources.Program.Error_InternalServerError)],
                        StatusCodes.Status500InternalServerError));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(token);
                logger.LogErrorMessage(ex, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new RequestResponse(localizer[nameof(Resources.Program.Error_InternalServerError)],
                        StatusCodes.Status500InternalServerError));
            }
        }

        return StatusCode(StatusCodes.Status409Conflict,
            new RequestResponse(localizer[nameof(Resources.Program.Error_InternalServerError)],
                StatusCodes.Status409Conflict));
    }

    private static bool IsPositiveFlagHit(ScoreResult? result) =>
        result?.MatchedFlagId.HasValue == true && result.ScoreChange > 0;

    private static bool IsPositiveFlagDuplicateConstraintViolation(ScoreResult? result, DbUpdateException exception) =>
        IsPositiveFlagHit(result)
        && exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: PositiveFlagUniqueIndexName
        };

    /// <summary>
    /// Queries flag status
    /// </summary>
    /// <remarks>
    /// Queries flag status; requires User permission
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="challengeId">Challenge ID</param>
    /// <param name="submitId">Submission ID</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved submission status</response>
    /// <response code="404">Submission not found</response>
    [RequireUser]
    [HttpGet("{id:int}/Challenges/{challengeId:int}/Status/{submitId:int}")]
    [ProducesResponseType(typeof(AnswerResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Status([FromRoute] int id, [FromRoute] int challengeId, [FromRoute] int submitId,
        CancellationToken token)
    {
        var claimId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (claimId is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_SubmissionNotFound)],
                StatusCodes.Status404NotFound));

        var submission =
            await submissionRepository.GetSubmission(id, challengeId, Guid.Parse(claimId), submitId, token);

        if (submission is null)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Submission_NotFound)],
                StatusCodes.Status404NotFound));

        return Ok(submission.Status switch
        {
            AnswerResult.CheatDetected => AnswerResult.WrongAnswer,
            var x => x
        });
    }

    /// <summary>
    /// Get writeup information
    /// </summary>
    /// <remarks>
    /// Retrieves post-game writeup submission information; requires User permission
    /// </remarks>
    /// <param name="id"></param>
    /// <param name="token"></param>
    /// <response code="200">Successfully submitted writeup</response>
    /// <response code="400">Submission does not meet requirements</response>
    /// <response code="404">Game not found</response>
    [RequireUser]
    [HttpGet("{id:int}/Writeup")]
    [ProducesResponseType(typeof(BasicWriteupInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetWriteup([FromRoute] int id, CancellationToken token)
    {
        var context = await GetContextInfo(id, denyAfterEnded: false, token: token);

        if (context.Result is not null)
            return context.Result;

        return Ok(BasicWriteupInfoModel.FromParticipation(context.Participation!));
    }

    /// <summary>
    /// Submits a writeup
    /// </summary>
    /// <remarks>
    /// Submits a post-game writeup; requires User permission
    /// </remarks>
    /// <param name="id"></param>
    /// <param name="file">File</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully submitted writeup</response>
    /// <response code="400">Submission does not meet requirements</response>
    /// <response code="404">Game not found</response>
    [RequireUser]
    [HttpPost("{id:int}/Writeup")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitWriteup([FromRoute] int id, IFormFile file, CancellationToken token)
    {
        var current = DateTimeOffset.UtcNow;
        switch (file.Length)
        {
            case 0:
                return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.File_SizeZero)]));
            case > 20 * 1024 * 1024:
                return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.File_SizeTooLarge)]));
        }

        if (file.ContentType != "application/pdf" || Path.GetExtension(file.FileName) != ".pdf")
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.File_PdfOnly)]));

        var context = await GetContextInfo(id, denyAfterEnded: false, token: token);

        if (context.Result is not null)
            return context.Result;

        var game = context.Game!;

        if (!game.WriteupRequired)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_WriteupNotNeeded)]));

        var part = context.Participation!;
        var team = part.Team;

        if (current > game.WriteupDeadline)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_DeadlineExpired)]));

        var wp = context.Participation!.Writeup;

        if (wp is not null)
            await blobService.DeleteBlob(wp, token);

        part.Writeup = await blobService.CreateOrUpdateBlob(file,
            $"Writeup-{game.Id}-{team.Id}-{current:yyyyMMdd-HH.mm.ss}Z.pdf", token);

        await participationRepository.SaveAsync(token);

        logger.Log(StaticLocalizer[nameof(Resources.Program.Game_WriteupSubmitted), team.Name, game.Title],
            context.User!,
            TaskStatus.Success);

        return Ok();
    }

    /// <summary>
    /// Creates a container
    /// </summary>
    /// <remarks>
    /// Creates a container; requires User permission
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="challengeId">Challenge ID</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved game challenge information</response>
    /// <response code="404">Challenge not found</response>
    /// <response code="400">Container creation not allowed for challenge</response>
    [RequireUser]
    [HttpPost("{id:int}/Container/{challengeId:int}")]
    [EnableRateLimiting(nameof(RateLimiter.LimitPolicy.Container))]
    [ProducesResponseType(typeof(ContainerInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> CreateContainer([FromRoute] int id, [FromRoute] int challengeId,
        CancellationToken token)
    {
        var context = await GetContextInfo(id, token: token);

        if (context.Result is not null)
            return context.Result;

        var permission = await divisionRepository.GetPermission(context.Participation?.DivisionId, challengeId, token);

        if (!permission.HasFlag(GamePermission.ViewChallenge))
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Challenge_NotFound)],
                StatusCodes.Status404NotFound));

        var instance = await gameInstanceRepository.GetInstance(context.Participation!, challengeId, token);

        if (instance is null || !instance.Challenge.IsEnabled)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Challenge_NotFound)],
                StatusCodes.Status404NotFound));

        if (!instance.Challenge.Type.IsContainer())
            return BadRequest(
                new RequestResponse(localizer[nameof(Resources.Program.Game_ContainerCreationNotAllowed)]));

        if (instance.IsContainerOperationTooFrequent)
            return RequestResponse.Result(localizer[nameof(Resources.Program.Game_OperationTooFrequent)],
                StatusCodes.Status429TooManyRequests);

        if (instance.Container is not null)
        {
            if (instance.Container.Status == ContainerStatus.Running)
                return BadRequest(
                    new RequestResponse(localizer[nameof(Resources.Program.Game_ContainerAlreadyCreated)]));

            await containerRepository.DestroyContainer(instance.Container, token);
        }

        return await gameInstanceRepository.CreateContainer(instance, context.Participation!.Team, context.User!,
                context.Game!, token) switch
        {
            null or (TaskStatus.Failed, null) => BadRequest(
                new RequestResponse(localizer[nameof(Resources.Program.Game_ContainerCreationFailed)])),
            (TaskStatus.Denied, null) => BadRequest(
                new RequestResponse(localizer[nameof(Resources.Program.Game_ContainerNumberLimitExceeded),
                    context.Game!.ContainerCountLimit])),
            (TaskStatus.Success, var x) => Ok(ContainerInfoModel.FromContainer(x!)),
            _ => throw new UnreachableException()
        };
    }

    /// <summary>
    /// Extends container lifetime
    /// </summary>
    /// <remarks>
    /// Extends container lifetime; requires User permission and can only be extended two hours within ten minutes before expiration
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="challengeId">Challenge ID</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully retrieved game challenge container information</response>
    /// <response code="404">Challenge not found</response>
    /// <response code="400">Container not created or cannot be extended</response>
    [RequireUser]
    [HttpPost("{id:int}/Container/{challengeId:int}/Extend")]
    [EnableRateLimiting(nameof(RateLimiter.LimitPolicy.Container))]
    [ProducesResponseType(typeof(ContainerInfoModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExtendContainerLifetime([FromRoute] int id, [FromRoute] int challengeId,
        CancellationToken token)
    {
        var context = await GetContextInfo(id, token: token);

        if (context.Result is not null)
            return context.Result;

        var instance = await gameInstanceRepository.GetInstance(context.Participation!, challengeId, token);

        if (instance is null || !instance.Challenge.IsEnabled)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Challenge_NotFound)],
                StatusCodes.Status404NotFound));

        if (!instance.Challenge.Type.IsContainer())
            return BadRequest(
                new RequestResponse(localizer[nameof(Resources.Program.Game_ContainerCreationNotAllowed)]));

        if (instance.Container is null)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_ContainerNotCreated)]));

        if (instance.Container.ExpectStopAt - DateTimeOffset.UtcNow >
            TimeSpan.FromMinutes(containerPolicy.Value.RenewalWindow))
            return BadRequest(
                new RequestResponse(localizer[nameof(Resources.Program.Game_ContainerExtensionNotAvailable)]));

        await containerRepository.ExtendLifetime(instance.Container,
            TimeSpan.FromMinutes(containerPolicy.Value.ExtensionDuration), token);

        return Ok(ContainerInfoModel.FromContainer(instance.Container));
    }

    /// <summary>
    /// Deletes a container
    /// </summary>
    /// <remarks>
    /// Deletes a container; requires User permission
    /// </remarks>
    /// <param name="id">Game ID</param>
    /// <param name="challengeId">Challenge ID</param>
    /// <param name="token"></param>
    /// <response code="200">Successfully deleted container</response>
    /// <response code="404">Challenge not found</response>
    /// <response code="400">Container creation not allowed for challenge</response>
    [RequireUser]
    [HttpDelete("{id:int}/Container/{challengeId:int}")]
    [EnableRateLimiting(nameof(RateLimiter.LimitPolicy.Container))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> DeleteContainer([FromRoute] int id, [FromRoute] int challengeId,
        CancellationToken token)
    {
        var context = await GetContextInfo(id, token: token);

        if (context.Result is not null)
            return context.Result;

        var instance = await gameInstanceRepository.GetInstance(context.Participation!, challengeId, token);

        if (instance is null || !instance.Challenge.IsEnabled)
            return NotFound(new RequestResponse(localizer[nameof(Resources.Program.Challenge_NotFound)],
                StatusCodes.Status404NotFound));

        if (!instance.Challenge.Type.IsContainer())
            return BadRequest(
                new RequestResponse(localizer[nameof(Resources.Program.Game_ContainerCreationNotAllowed)]));

        if (instance.Container is null)
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_ContainerNotCreated)]));

        if (instance.IsContainerOperationTooFrequent)
            return RequestResponse.Result(localizer[nameof(Resources.Program.Game_OperationTooFrequent)],
                StatusCodes.Status429TooManyRequests);

        var destroyId = instance.Container.LogId;

        if (!await containerRepository.DestroyContainer(instance.Container, token))
            return BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_ContainerDeletionFailed)]));

        instance.LastContainerOperation = DateTimeOffset.UtcNow;

        await gameEventRepository.AddEvent(
            new()
            {
                Type = EventType.ContainerDestroy,
                GameId = context.Game!.Id,
                TeamId = context.Participation!.TeamId,
                UserId = context.User!.Id,
                Values = [instance.Challenge.Id.ToString(), instance.Challenge.Title]
            }, token);

        logger.Log(
            StaticLocalizer[nameof(Resources.Program.Game_ContainerDeleted), context.Participation!.Team.Name,
                instance.Challenge.Title,
                destroyId],
            context.User, TaskStatus.Success);

        return Ok();
    }

    private async Task<ContextInfo> GetContextInfo(int id, bool denyAfterEnded = true,
        CancellationToken token = default)
    {
        ContextInfo res = new()
        {
            User = await userManager.GetUserAsync(User),
            Game = await gameRepository.GetGameById(id, token)
        };

        if (res.Game is null)
            return res.WithResult(NotFound(new RequestResponse(localizer[nameof(Resources.Program.Game_NotFound)],
                StatusCodes.Status404NotFound)));

        var part = await participationRepository.GetParticipation(res.User!.Id, res.Game.Id, token);

        if (part is null)
            return res.WithResult(
                BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_NotParticipated)])));

        res.Participation = part;

        if (part.Status != ParticipationStatus.Accepted)
            return res.WithResult(
                BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_ParticipationNotAccepted)])));

        if (DateTimeOffset.UtcNow < res.Game.StartTimeUtc)
            return res.WithResult(
                BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_NotStarted),
                    ErrorCodes.GameNotStarted])));

        if (denyAfterEnded && !res.Game.PracticeMode && res.Game.EndTimeUtc < DateTimeOffset.UtcNow)
            return res.WithResult(
                BadRequest(new RequestResponse(localizer[nameof(Resources.Program.Game_Ended)], ErrorCodes.GameEnded)));

        return res;
    }

    private static string GameETag(int gameId, DateTimeOffset lastModified) =>
        $"\"{gameId}-{lastModified.ToUnixTimeSeconds():X}\"";

    private static Dictionary<ChallengeCategory, IEnumerable<ChallengeInfo>> FilterChallengesByPermission(
        Dictionary<ChallengeCategory, IEnumerable<ChallengeInfo>> challenges,
        DivisionItem division)
    {
        var res = new Dictionary<ChallengeCategory, IEnumerable<ChallengeInfo>>();

        foreach ((var cat, var chs) in challenges)
        {
            var infos = chs.Where(chal =>
                division.ChallengeConfigs.TryGetValue(chal.Id, out var config)
                    ? config.Permissions.HasFlag(GamePermission.ViewChallenge)
                    : division.DefaultPermissions.HasFlag(GamePermission.ViewChallenge)
            ).ToArray();

            if (infos.Length > 0)
                res[cat] = infos;
        }

        return res;
    }

    private class ContextInfo
    {
        public Game? Game;
        public Participation? Participation;
        public UserInfo? User;

        /// <summary>
        /// The result to be returned.
        /// If this is not null, the action should return this result directly.
        /// </summary>
        public IActionResult? Result;

        public ContextInfo WithResult(IActionResult res)
        {
            Result = res;
            return this;
        }
    }
}
