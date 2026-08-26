using Mercato.Application.DTOs;
using Mercato.Application.Repositories;
using Mercato.Domain.Entities;

namespace Mercato.Application.Services;

public sealed class ArtistServiceImplementation : IArtistService
{
    private readonly IArtistRepository _artists;

    public ArtistServiceImplementation(IArtistRepository artists)
    {
        _artists = artists;
    }

    public async Task<IReadOnlyList<ArtistDto>> GetAllAsync(CancellationToken cancellationToken = default)
        => (await _artists.GetAllAsync(cancellationToken)).Select(Map).ToArray();

    public async Task<ArtistDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var artist = await _artists.GetAsync(id, cancellationToken);
        return artist is null ? null : Map(artist);
    }

    public async Task<ArtistDto> CreateAsync(CreateArtistRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request.Name);
        return Map(await _artists.AddAsync(new Artist
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Phone = Normalize(request.Phone)
        }, cancellationToken));
    }

    public async Task<ArtistDto?> UpdateAsync(Guid id, UpdateArtistRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request.Name);
        var artist = await _artists.GetAsync(id, cancellationToken);
        if (artist is null) return null;
        artist.Name = request.Name.Trim();
        artist.Phone = Normalize(request.Phone);
        var updated = await _artists.UpdateAsync(artist, cancellationToken);
        return updated is null ? null : Map(updated);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => _artists.DeleteAsync(id, cancellationToken);

    private static void Validate(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Artist name is required.", nameof(name));
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static ArtistDto Map(Artist artist) => new(artist.Id, artist.Name, artist.Phone);
}
