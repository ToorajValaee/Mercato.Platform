using Mercato.Application.DTOs;
using Mercato.Application.Interfaces;
using Mercato.Application.Repositories;
using Mercato.Domain.Entities;

namespace Mercato.Application.Services;

public sealed class BranchTransferServiceImplementation : IBranchTransferService
{
    private readonly IBranchTransferRepository _transfers;
    private readonly IBranchRepository _branches;
    private readonly IProductRepository _products;
    private readonly IInventoryService _inventory;
    private readonly IUnitOfWork _unitOfWork;

    public BranchTransferServiceImplementation(
        IBranchTransferRepository transfers,
        IBranchRepository branches,
        IProductRepository products,
        IInventoryService inventory,
        IUnitOfWork unitOfWork)
    {
        _transfers = transfers;
        _branches = branches;
        _products = products;
        _inventory = inventory;
        _unitOfWork = unitOfWork;
    }

    public Task<BranchTransferDto> CreateAsync(
        CreateBranchTransferRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.SourceBranchId == Guid.Empty || request.DestinationBranchId == Guid.Empty)
            throw new ArgumentException("Source and destination branches are required.");
        if (request.SourceBranchId == request.DestinationBranchId)
            throw new ArgumentException("Source and destination branches must be different.");
        if (request.ProductId == Guid.Empty || request.Quantity <= 0)
            throw new ArgumentException("Product and positive quantity are required.");

        return _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            if (await _branches.GetAsync(request.SourceBranchId, ct) is null)
                throw new InvalidOperationException("Source branch was not found.");
            if (await _branches.GetAsync(request.DestinationBranchId, ct) is null)
                throw new InvalidOperationException("Destination branch was not found.");
            if (await _products.GetByIdAsync(request.ProductId, ct) is null)
                throw new InvalidOperationException("Product was not found.");

            await _inventory.TransferStockAsync(
                request.ProductId,
                request.SourceBranchId,
                request.DestinationBranchId,
                request.Quantity);

            var transfer = await _transfers.AddAsync(new BranchTransfer
            {
                Id = Guid.NewGuid(),
                SourceBranchId = request.SourceBranchId,
                DestinationBranchId = request.DestinationBranchId,
                ProductId = request.ProductId,
                Quantity = request.Quantity,
                CreatedAt = DateTime.UtcNow
            }, ct);

            return Map(transfer);
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<BranchTransferDto>> GetAllAsync(
        Guid? branchId = null,
        CancellationToken cancellationToken = default)
    {
        var transfers = await _transfers.GetAllAsync(branchId, cancellationToken);
        return transfers.Select(Map).ToArray();
    }

    private static BranchTransferDto Map(BranchTransfer transfer)
        => new(
            transfer.Id,
            transfer.SourceBranchId,
            transfer.DestinationBranchId,
            transfer.ProductId,
            transfer.Quantity,
            transfer.CreatedAt);
}
