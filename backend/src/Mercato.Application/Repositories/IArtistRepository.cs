using Mercato.Domain.Entities;

namespace Mercato.Application.Repositories;

public interface IArtistRepository
{
    Task<IReadOnlyList<Artist>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Artist?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Artist> AddAsync(Artist artist, CancellationToken cancellationToken = default);
    Task<Artist?> UpdateAsync(Artist artist, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
