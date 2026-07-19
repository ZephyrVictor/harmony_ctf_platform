using System.Net.Mime;
using System.Text.Json;
using GZCTF.Middlewares;
using GZCTF.Models;
using GZCTF.Services.Import;
using Microsoft.AspNetCore.Mvc;

namespace GZCTF.Controllers;

[ApiController]
[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(RequestResponse), StatusCodes.Status403Forbidden)]
public class FlagImportController(FlagImportService importService) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// 预览单题目导入（不传 mapping 仅返回列名+样本行；传 mapping 试解析 Track 预览）
    /// </summary>
    [RequireAdmin]
    [HttpPost("api/ext/challenges/{challengeId:int}/flags/import/preview")]
    [ProducesResponseType(typeof(FlagImportFilePreview), StatusCodes.Status200OK)]
    public async Task<IActionResult> PreviewChallenge([FromRoute] int challengeId, IFormFile file,
        [FromForm] string? mapping, CancellationToken token)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new RequestResponse("File is required"));

        FlagImportColumnMapping? mappingObj = null;
        if (!string.IsNullOrWhiteSpace(mapping))
        {
            try
            {
                mappingObj = JsonSerializer.Deserialize<FlagImportColumnMapping>(mapping, JsonOptions);
            }
            catch (JsonException ex)
            {
                return BadRequest(new RequestResponse($"Invalid mapping JSON: {ex.Message}"));
            }
        }

        await using var stream = file.OpenReadStream();
        var preview = await importService.PreviewFileAsync(stream, file.ContentType, file.FileName, mappingObj, token);
        return Ok(preview);
    }

    /// <summary>
    /// 单题目导入 flags
    /// </summary>
    [RequireAdmin]
    [HttpPost("api/ext/challenges/{challengeId:int}/flags/import")]
    [ProducesResponseType(typeof(FlagImportResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ImportChallenge([FromRoute] int challengeId, IFormFile file,
        [FromForm] string mapping, [FromForm] string mode, CancellationToken token)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new RequestResponse("File is required"));

        if (string.IsNullOrWhiteSpace(mapping))
            return BadRequest(new RequestResponse("mapping is required"));

        if (string.IsNullOrWhiteSpace(mode))
            return BadRequest(new RequestResponse("mode is required"));

        FlagImportColumnMapping? mappingObj;
        try
        {
            mappingObj = JsonSerializer.Deserialize<FlagImportColumnMapping>(mapping, JsonOptions);
        }
        catch (JsonException ex)
        {
            return BadRequest(new RequestResponse($"Invalid mapping JSON: {ex.Message}"));
        }

        if (mappingObj is null)
            return BadRequest(new RequestResponse("Invalid mapping JSON"));

        if (!Enum.TryParse<ImportMode>(mode, ignoreCase: true, out var modeEnum))
            return BadRequest(new RequestResponse(
                $"Invalid mode '{mode}'. Valid values: {string.Join(", ", Enum.GetNames<ImportMode>())}"));

        await using var stream = file.OpenReadStream();
        var result = await importService.ImportFlagsAsync(
            challengeId, stream, file.ContentType, file.FileName, mappingObj, modeEnum, token);
        return Ok(result);
    }

    /// <summary>
    /// 游戏级预览：按 (仓库, 子模块) 自动建题目的预览
    /// </summary>
    [RequireAdmin]
    [HttpPost("api/ext/games/{gameId:int}/flags/import/preview")]
    [ProducesResponseType(typeof(FlagImportFilePreview), StatusCodes.Status200OK)]
    public async Task<IActionResult> PreviewGame([FromRoute] int gameId, IFormFile file,
        [FromForm] string? mapping, CancellationToken token)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new RequestResponse("File is required"));

        FlagImportColumnMapping? mappingObj = null;
        if (!string.IsNullOrWhiteSpace(mapping))
        {
            try
            {
                mappingObj = JsonSerializer.Deserialize<FlagImportColumnMapping>(mapping, JsonOptions);
            }
            catch (JsonException ex)
            {
                return BadRequest(new RequestResponse($"Invalid mapping JSON: {ex.Message}"));
            }
        }

        await using var stream = file.OpenReadStream();
        var preview = await importService.PreviewFileAsync(stream, file.ContentType, file.FileName, mappingObj, token);
        return Ok(preview);
    }

    /// <summary>
    /// 游戏级导入：按 (仓库, 子模块) 自动创建 GameChallenge 并导入 flags
    /// </summary>
    [RequireAdmin]
    [HttpPost("api/ext/games/{gameId:int}/flags/import")]
    [ProducesResponseType(typeof(FlagImportResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ImportGame([FromRoute] int gameId, IFormFile file,
        [FromForm] string mapping, [FromForm] string mode, CancellationToken token)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new RequestResponse("File is required"));

        if (string.IsNullOrWhiteSpace(mapping))
            return BadRequest(new RequestResponse("mapping is required"));

        if (string.IsNullOrWhiteSpace(mode))
            return BadRequest(new RequestResponse("mode is required"));

        FlagImportColumnMapping? mappingObj;
        try
        {
            mappingObj = JsonSerializer.Deserialize<FlagImportColumnMapping>(mapping, JsonOptions);
        }
        catch (JsonException ex)
        {
            return BadRequest(new RequestResponse($"Invalid mapping JSON: {ex.Message}"));
        }

        if (mappingObj is null)
            return BadRequest(new RequestResponse("Invalid mapping JSON"));

        if (string.IsNullOrWhiteSpace(mappingObj.RepositoryColumn) || string.IsNullOrWhiteSpace(mappingObj.SubmoduleColumn))
            return BadRequest(new RequestResponse("RepositoryColumn and SubmoduleColumn are required for game-level import"));

        if (!Enum.TryParse<ImportMode>(mode, ignoreCase: true, out var modeEnum))
            return BadRequest(new RequestResponse(
                $"Invalid mode '{mode}'. Valid values: {string.Join(", ", Enum.GetNames<ImportMode>())}"));

        await using var stream = file.OpenReadStream();
        var result = await importService.ImportFlagsToGameAsync(
            gameId, stream, file.ContentType, file.FileName, mappingObj, modeEnum, token);
        return Ok(result);
    }
}
