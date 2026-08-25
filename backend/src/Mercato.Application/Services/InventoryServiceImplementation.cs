using Mercato.Application.Repositories;

namespace Mercato.Application.Services;

public sealed class InventoryServiceImplementation : IInventoryService
{
    private readonly IInventoryRepository _inventoryRepository;

    public InventoryServiceImplementation(IInventoryRepository inventoryRepository)
    {
        _inventoryRepository = inventoryRepository;
    }

    public Task<int> GetAvailableQuantityAsync(Guid productId, Guid branchId)
    {
        return _inventoryRepository.GetAvailableQuantityAsync(branchId, productId);
    }

    public async Task AdjustStockAsync(Guid productId, Guid branchId, decimal quantity, string reason)
    {
        if (quantity == 0)
        {
            throw new ArgumentException("Quantity cannot be zero.", nameof(quantity));
        }

        if (quantity < 0)
        {
            var available = await _inventoryRepository.GetAvailableQuantityAsync(branchId, productId);
            if (available + quantity < 0)
            {
                throw new InvalidOperationException("Insufficient stock.");
            }
        }

        await _inventoryRepository.AddMovementAsync(
            branchId,
            productId,
            (int)quantity,
            reason);
    }

    public async Task TransferStockAsync(Guid productId, Guid fromBranchId, Guid toBranchId, decimal quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("Transfer quantity must be positive.", nameof(quantity));
        }

        var available = await _inventoryRepository.GetAvailableQuantityAsync(fromBranchId, productId);
        if (available < quantity)
        {
            throw new InvalidOperationException("Insufficient source stock.");
        }

        await _inventoryRepository.AddMovementAsync(fromBranchId, productId, -(int)quantity, "Transfer-Out");
        await _inventoryRepository.AddMovementAsync(toBranchId, productId, (int)quantity, "Transfer-In");
    }
}
