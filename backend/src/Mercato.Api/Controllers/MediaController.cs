using Mercato.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mercato.Api.Controllers;

[ApiController]
[Route("api/media")]
public sealed class MediaController : ControllerBase
{
    private readonly IProductMediaStorage _storage;

    public MediaController(IProductMediaStorage storage)
    {
        _storage = storage;
    }

    [HttpPost("product-image")]
    [Authorize(Roles = "Admin,Manager")]
    [RequestSizeLimit(12 * 1024 * 1024)]
    public async Task<IActionResult> UploadProductImage(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length <= 0) return BadRequest(new { error = "Image file is required." });
        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Only image files are accepted." });

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _storage.SaveProductImageAsync(stream, file.FileName, file.ContentType, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{**objectName}")]
    [AllowAnonymous]
    public async Task<IActionResult> Get(string objectName, CancellationToken cancellationToken)
    {
        if (objectName.Contains("..", StringComparison.Ordinal) || !objectName.StartsWith("products/", StringComparison.Ordinal))
            return BadRequest();
        var media = await _storage.OpenAsync(objectName, cancellationToken);
        return media is null ? NotFound() : File(media.Content, media.ContentType);
    }
}
