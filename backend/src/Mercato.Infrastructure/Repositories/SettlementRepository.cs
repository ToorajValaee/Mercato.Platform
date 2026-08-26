using Microsoft.EntityFrameworkCore;
using Mercato.Application.Repositories;
using Mercato.Domain.Entities;
using Mercato.Infrastructure.Data;

namespace Mercato.Infrastructure.Repositories;

public sealed class SettlementRepository : ISettlementRepository
{
    private readonly MercatoDbContext _context;

    public SettlementRepository(MercatoDbContext context)
    {
        _context = context;
    }

    public async Task AddLineAsync(SettlementLine line, CancellationToken cancellationToken = default)
    {
        _context.SettlementLines.Add(line);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SettlementLine>> GetLinesAsync(
        Guid artistId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        return await _context.SettlementLines
            .AsNoTracking()
            .Where(x => x.ArtistId == artistId)
            .Where(x => _context.Orders.Any(order =>
                order.Id == x.OrderId &&
                order.CreatedAtUtc >= from &&
                order.CreatedAtUtc < to))
            .ToListAsync(cancellationToken);
    }
}
