using Mercato.Application.DTOs;
using Mercato.Application.Repositories;

namespace Mercato.Application.Services;

public sealed class InventoryServiceImplementation : IInventoryService
{
    private readonly IInventoryRepository _inventory;
    private readonly IProductRepository _products;
    private readonly IBranchRepository _branches;

    public InventoryServiceImplementation(
        IInventoryRepository inventory,
        IProductRepository products,
        IBranchRepository branches)
    {
        _inventory = inventory;
        _products = products;
        _branches = branches;
    }

    public async Task<int> GetAvailableQuantityAsync(
        Guid productId,
        Guid branchId,
        CancellationToken cancellationToken = default)
    {
        await ValidateReferencesAsync(productId, branchId, cancellationToken);
        return await _inventory.GetAvailableQuantityAsync(branchId, productId, cancellationToken);
    }

    public async Task AdjustStockAsync(
        Guid productId,
        Guid branchId,
        decimal quantity,
        string reason,
        CancellationToken cancellationToken = default)
    {
        await ValidateReferencesAsync(productId, branchId, cancellationToken);
        var integralQuantity = ValidateIntegralQuantity(quantity);
        if (integralQuantity < 0)
        {
            var available = await _inventory.GetAvailableQuantityAsync(branchId, productId, cancellationToken);
            if (available + integralQuantity < 0)
                throw new InvalidOperationException("Insufficient stock.");
        }

        await _inventory.AddMovementAsync(
            branchId,
            productId,
            integralQuantity,
            string.IsNullOrWhiteSpace(reason) ? "Adjustment" : reason,
            cancellationToken);
    }

    public async Task TransferStockAsync(
        Guid productId,
        Guid fromBranchId,
        Guid toBranchId,
        decimal quantity,
        CancellationToken cancellationToken = default)
    {
        if (fromBranchId == toBranchId)
            throw new ArgumentException("Source and destination branches must be different.");

        await ValidateReferencesAsync(productId, fromBranchId, cancellationToken);
        if (await _branches.GetAsync(toBranchId, cancellationToken) is null)
            throw new InvalidOperationException("Destination branch was not found.");

        var integralQuantity = ValidateIntegralQuantity(quantity);
        if (integralQuantity <= 0)
            throw new ArgumentException("Transfer quantity must be positive.", nameof(quantity));

        var available = await _inventory.GetAvailableQuantityAsync(fromBranchId, productId, cancellationToken);
        if (available < integralQuantity)
            throw new InvalidOperationException("Insufficient source stock.");

        await _inventory.AddMovementAsync(fromBranchId, productId, -integralQuantity, "Transfer-Out", cancellationToken);
        await _inventory.AddMovementAsync(toBranchId, productId, integralQuantity, "Transfer-In", cancellationToken);
    }

    public async Task<IReadOnlyList<StockMovementDto>> GetMovementsAsync(
        Guid? branchId = null,
        Guid? productId = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        CancellationToken cancellationToken = default)
    {
        if (fromUtc is DateTime from && toUtc is DateTime to && to <= from)
            throw new ArgumentException("Inventory history end date must be after start date.");

        var rows = await _inventory.GetMovementsAsync(branchId, productId, fromUtc, toUtc, cancellationToken);
        return rows.Select(x => new StockMovementDto(
            x.Id,
            x.BranchId,
            x.ProductId,
            x.Quantity,
            x.Type,
            x.CreatedAtUtc)).ToArray();
    }

    private async Task ValidateReferencesAsync(Guid productId, Guid branchId, CancellationToken cancellationToken)
    {
        if (productId == Guid.Empty || await _products.GetByIdAsync(productId, cancellationToken) is null)
            throw new InvalidOperationException("Product was not found.");
        if (branchId == Guid.Empty || await _branches.GetAsync(branchId, cancellationToken) is null)
            throw new InvalidOperationException("Branch was not found.");
    }

    private static int ValidateIntegralQuantity(decimal quantity)
    {
        if (quantity == 0) throw new ArgumentException("Quantity cannot be zero.", nameof(quantity));
        if (decimal.Truncate(quantity) != quantity) throw new ArgumentException("Inventory quantity must be a whole number.", nameof(quantity));
        return checked((int)quantity);
    }
}
