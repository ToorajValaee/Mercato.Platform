using Microsoft.EntityFrameworkCore;
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

    public async Task<int> GetAvailableQuantityAsync(Guid branchId, Guid productId)
    {
        var quantity = await _context.StockMovements
            .Where(x => x.BranchId == branchId && x.ProductId == productId)
            .SumAsync(x => x.Quantity);

        return checked((int)quantity);
    }

    public async Task AddMovementAsync(Guid branchId, Guid productId, int quantity, string movementType)
    {
        if (branchId == Guid.Empty || productId == Guid.Empty)
            throw new ArgumentException("Branch and product are required for an inventory movement.");

        if (quantity == 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        _context.StockMovements.Add(new StockMovement
        {
            Id = Guid.NewGuid(),
            BranchId = branchId,
            ProductId = productId,
            Quantity = quantity,
            Type = string.IsNullOrWhiteSpace(movementType) ? "Adjustment" : movementType.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
    }
}
