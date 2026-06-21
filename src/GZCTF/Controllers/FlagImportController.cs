using System.Net.Mime;
using System.Text.Json;
using GZCTF.Middlewares;
using GZCTF.Models;
using GZCTF.Services.Import;
using Microsoft.AspNetCore.Mvc;

namespace GZCTF.Controllers;

[ApiController]
[Route("api/ext/challenges/{challengeId:int}/flags/import")]
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

    [RequireAdmin]
    [HttpPost("preview")]
    [ProducesResponseType(typeof(FlagImportFilePreview), StatusCodes.Status200OK)]
    public async Task<IActionResult> Preview([FromRoute] int challengeId, IFormFile file, CancellationToken token)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new RequestResponse("File is required"));

        await using var stream = file.OpenReadStream();
        var preview = await importService.PreviewFileAsync(stream, file.ContentType, token);
        return Ok(preview);
    }

    [RequireAdmin]
    [HttpPost]
    [ProducesResponseType(typeof(FlagImportResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Import([FromRoute] int challengeId, IFormFile file,
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
        var result = await importService.ImportFlagsAsync(challengeId, stream, file.ContentType, mappingObj, modeEnum, token);
        return Ok(result);
    }
}
