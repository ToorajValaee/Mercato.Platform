using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Mercato.Application.Repositories;
using Mercato.Domain.Entities;
using Mercato.Infrastructure.Data;

namespace Mercato.Infrastructure.Repositories;

public sealed class InventoryRepository : IInventoryRepository
{
    private readonly MercatoDbContext _context;

    public InventoryRepository(MercatoDbContext context)
    {
        _context = context;
    }

    public async Task<int> GetAvailableQuantityAsync(
        Guid branchId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var quantity = await _context.StockMovements
            .Where(x => x.BranchId == branchId && x.ProductId == productId)
            .SumAsync(x => x.Quantity, cancellationToken);
        return checked((int)quantity);
    }

    public async Task AddMovementAsync(
        Guid branchId,
        Guid productId,
        int quantity,
        string movementType,
        bool preventNegativeBalance = false,
        CancellationToken cancellationToken = default)
    {
        if (branchId == Guid.Empty || productId == Guid.Empty)
            throw new ArgumentException("Branch and product are required for an inventory movement.");
        if (quantity == 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        IDbContextTransaction? ownedTransaction = null;
        if (_context.Database.CurrentTransaction is null)
            ownedTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Serialize every stock mutation for the same branch/product pair. This prevents two
            // concurrent checkouts from both validating against the same ledger snapshot and then
            // inserting deductions that drive stock below zero. The lock lives until the ambient
            // transaction commits (or until this method's transaction commits when called alone).
            var lockKey = $"{branchId:N}:{productId:N}";
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0));",
                cancellationToken);

            if (preventNegativeBalance && quantity < 0)
            {
                var available = await _context.StockMovements
                    .Where(x => x.BranchId == branchId && x.ProductId == productId)
                    .SumAsync(x => x.Quantity, cancellationToken);

                if (available + quantity < 0)
                    throw new InvalidOperationException("Insufficient stock.");
            }

            _context.StockMovements.Add(new StockMovement
            {
                Id = Guid.NewGuid(),
                BranchId = branchId,
                ProductId = productId,
                Quantity = quantity,
                Type = string.IsNullOrWhiteSpace(movementType) ? "Adjustment" : movementType.Trim(),
                CreatedAtUtc = DateTime.UtcNow
            });
            await _context.SaveChangesAsync(cancellationToken);

            if (ownedTransaction is not null)
                await ownedTransaction.CommitAsync(cancellationToken);
        }
        catch
        {
            if (ownedTransaction is not null)
                await ownedTransaction.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (ownedTransaction is not null)
                await ownedTransaction.DisposeAsync();
        }
    }

    public async Task<IReadOnlyList<StockMovement>> GetMovementsAsync(
        Guid? branchId = null,
        Guid? productId = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.StockMovements.AsNoTracking().AsQueryable();
        if (branchId is Guid branch && branch != Guid.Empty) query = query.Where(x => x.BranchId == branch);
        if (productId is Guid product && product != Guid.Empty) query = query.Where(x => x.ProductId == product);
        if (fromUtc is DateTime from) query = query.Where(x => x.CreatedAtUtc >= from);
        if (toUtc is DateTime to) query = query.Where(x => x.CreatedAtUtc < to);
        return await query.OrderByDescending(x => x.CreatedAtUtc).ToListAsync(cancellationToken);
    }
}
