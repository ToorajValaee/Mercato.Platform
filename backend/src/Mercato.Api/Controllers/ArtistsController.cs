using Mercato.Application.DTOs;
using Mercato.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mercato.Api.Controllers;

[ApiController]
[Route("api/artists")]
[Authorize]
public sealed class ArtistsController : ControllerBase
{
    private readonly IArtistService _artists;

    public ArtistsController(IArtistService artists)
    {
        _artists = artists;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
        => Ok(await _artists.GetAllAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var artist = await _artists.GetAsync(id, cancellationToken);
        return artist is null ? NotFound() : Ok(artist);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Create(CreateArtistRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var artist = await _artists.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(Get), new { id = artist.Id }, artist);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Update(Guid id, UpdateArtistRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var artist = await _artists.UpdateAsync(id, request, cancellationToken);
            return artist is null ? NotFound() : Ok(artist);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return await _artists.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { error = exception.Message });
        }
    }
}
