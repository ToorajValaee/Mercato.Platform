using Mercato.Application.DTOs;

namespace Mercato.Application.Services;

public interface IArtistService
{
    Task<IReadOnlyList<ArtistDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ArtistDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ArtistDto> CreateAsync(CreateArtistRequest request, CancellationToken cancellationToken = default);
    Task<ArtistDto?> UpdateAsync(Guid id, UpdateArtistRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
