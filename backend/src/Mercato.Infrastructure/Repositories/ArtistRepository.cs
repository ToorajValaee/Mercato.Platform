using Microsoft.EntityFrameworkCore;
using Mercato.Application.Repositories;
using Mercato.Domain.Entities;
using Mercato.Infrastructure.Data;

namespace Mercato.Infrastructure.Repositories;

public sealed class ArtistRepository : IArtistRepository
{
    private readonly MercatoDbContext _context;

    public ArtistRepository(MercatoDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Artist>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.Artists.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);

    public Task<Artist?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Artists.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<Artist> AddAsync(Artist artist, CancellationToken cancellationToken = default)
    {
        _context.Artists.Add(artist);
        await _context.SaveChangesAsync(cancellationToken);
        return artist;
    }

    public async Task<Artist?> UpdateAsync(Artist artist, CancellationToken cancellationToken = default)
    {
        if (!await _context.Artists.AnyAsync(x => x.Id == artist.Id, cancellationToken)) return null;
        _context.Artists.Update(artist);
        await _context.SaveChangesAsync(cancellationToken);
        return artist;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var artist = await _context.Artists.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (artist is null) return false;
        if (await _context.Products.AnyAsync(x => x.ArtistId == id, cancellationToken))
            throw new InvalidOperationException("Artist cannot be deleted while products reference the artist.");
        _context.Artists.Remove(artist);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
