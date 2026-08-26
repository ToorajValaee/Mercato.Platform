using Microsoft.EntityFrameworkCore;
using Mercato.Application.Repositories;
using Mercato.Domain.Entities;
using Mercato.Infrastructure.Data;

namespace Mercato.Infrastructure.Repositories;

public sealed class CheckoutIdempotencyRepository : ICheckoutIdempotencyRepository
{
    private readonly MercatoDbContext _context;

    public CheckoutIdempotencyRepository(MercatoDbContext context)
    {
        _context = context;
    }

    public Task<CheckoutIdempotencyRecord?> GetAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return _context.CheckoutIdempotencyRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    public async Task AddAsync(
        CheckoutIdempotencyRecord record,
        CancellationToken cancellationToken = default)
    {
        _context.CheckoutIdempotencyRecords.Add(record);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            throw new CheckoutIdempotencyConflictException(record.IdempotencyKey, exception);
        }
    }
}
