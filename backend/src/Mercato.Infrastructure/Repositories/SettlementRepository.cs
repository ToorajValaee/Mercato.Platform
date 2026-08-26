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

    public async Task<IReadOnlyList<SettlementLine>> GetLinesAsync(Guid artistId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        return await _context.SettlementLines
            .AsNoTracking()
            .Where(x => x.ArtistId == artistId)
            .Where(x => _context.Orders.Any(order => order.Id == x.OrderId && order.CreatedAtUtc >= from && order.CreatedAtUtc < to))
            .ToListAsync(cancellationToken);
    }

    public Task<ArtistSettlement?> GetForPeriodAsync(Guid artistId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
        => _context.ArtistSettlements.AsNoTracking().FirstOrDefaultAsync(
            x => x.ArtistId == artistId && x.PeriodFromUtc == from && x.PeriodToUtc == to,
            cancellationToken);

    public Task<ArtistSettlement?> GetSettlementAsync(Guid settlementId, CancellationToken cancellationToken = default)
        => _context.ArtistSettlements.AsNoTracking().FirstOrDefaultAsync(x => x.Id == settlementId, cancellationToken);

    public async Task<ArtistSettlement> AddSettlementAsync(ArtistSettlement settlement, CancellationToken cancellationToken = default)
    {
        _context.ArtistSettlements.Add(settlement);
        await _context.SaveChangesAsync(cancellationToken);
        return settlement;
    }

    public async Task<IReadOnlyList<ArtistSettlement>> GetSettlementsAsync(Guid? artistId = null, bool? isPaid = null, CancellationToken cancellationToken = default)
    {
        var query = _context.ArtistSettlements.AsNoTracking().AsQueryable();
        if (artistId.HasValue) query = query.Where(x => x.ArtistId == artistId.Value);
        if (isPaid.HasValue) query = query.Where(x => x.IsPaid == isPaid.Value);
        return await query.OrderByDescending(x => x.PeriodToUtc).ThenBy(x => x.ArtistId).ToListAsync(cancellationToken);
    }

    public async Task<ArtistSettlement?> MarkPaidAsync(Guid settlementId, DateTime paidAtUtc, CancellationToken cancellationToken = default)
    {
        var settlement = await _context.ArtistSettlements.FirstOrDefaultAsync(x => x.Id == settlementId, cancellationToken);
        if (settlement is null) return null;
        settlement.IsPaid = true;
        settlement.PaidAtUtc = paidAtUtc;
        await _context.SaveChangesAsync(cancellationToken);
        return settlement;
    }
}
