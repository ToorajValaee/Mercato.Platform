using Mercato.Api.Services;
using Mercato.Application.Interfaces;
using Mercato.Application.Repositories;
using Mercato.Application.Services;
using Mercato.Domain.Entities;
using Mercato.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Api.Controllers;

[ApiController]
[Route("api/goods-receipts")]
[Authorize(Roles = "Admin,Manager")]
public sealed class GoodsReceiptsController : ControllerBase
{
    private readonly MercatoDbContext _db;
    private readonly IArtistRepository _artists;
    private readonly IProductRepository _products;
    private readonly IBranchRepository _branches;
    private readonly IInventoryService _inventory;
    private readonly IUnitOfWork _unitOfWork;
    private readonly CurrentUserBranchAccess _branchAccess;

    public GoodsReceiptsController(
        MercatoDbContext db,
        IArtistRepository artists,
        IProductRepository products,
        IBranchRepository branches,
        IInventoryService inventory,
        IUnitOfWork unitOfWork,
        CurrentUserBranchAccess branchAccess)
    {
        _db = db;
        _artists = artists;
        _products = products;
        _branches = branches;
        _inventory = inventory;
        _unitOfWork = unitOfWork;
        _branchAccess = branchAccess;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] Guid? artistId, [FromQuery] Guid? branchId, CancellationToken cancellationToken)
    {
        if (branchId is Guid branch && !await _branchAccess.CanAccessAsync(branch, cancellationToken)) return Forbid();

        var query = _db.GoodsReceipts.AsNoTracking().Include(x => x.Items).AsQueryable();
        if (artistId is Guid artist && artist != Guid.Empty) query = query.Where(x => x.ArtistId == artist);
        if (branchId is Guid selectedBranch && selectedBranch != Guid.Empty) query = query.Where(x => x.BranchId == selectedBranch);

        var receipts = await query.OrderByDescending(x => x.CreatedAtUtc).Take(500).ToListAsync(cancellationToken);
        var artists = await _artists.GetAllAsync(cancellationToken);
        var branches = await _branches.GetAllAsync(cancellationToken);
        var products = await _products.GetAllAsync(cancellationToken);

        var accessible = new List<object>();
        foreach (var receipt in receipts)
        {
            if (!await _branchAccess.CanAccessAsync(receipt.BranchId, cancellationToken)) continue;
            accessible.Add(new
            {
                receipt.Id,
                receipt.ArtistId,
                artistName = artists.FirstOrDefault(x => x.Id == receipt.ArtistId)?.Name,
                receipt.BranchId,
                branchName = branches.FirstOrDefault(x => x.Id == receipt.BranchId)?.Name,
                receipt.Reference,
                receipt.CreatedAtUtc,
                items = receipt.Items.Select(line => new
                {
                    line.ProductId,
                    productName = products.FirstOrDefault(x => x.Id == line.ProductId)?.Name ?? line.ProductId.ToString(),
                    line.Quantity,
                    line.PurchaseUnitPrice,
                    totalPurchaseCost = line.Quantity * line.PurchaseUnitPrice
                })
            });
        }
        return Ok(accessible);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateGoodsReceiptRequest request, CancellationToken cancellationToken)
    {
        if (request.ArtistId == Guid.Empty) return BadRequest(new { error = "Artist is required." });
        if (request.BranchId == Guid.Empty) return BadRequest(new { error = "Branch is required." });
        if (!await _branchAccess.CanAccessAsync(request.BranchId, cancellationToken)) return Forbid();
        if (await _artists.GetAsync(request.ArtistId, cancellationToken) is null) return BadRequest(new { error = "Artist was not found." });
        if (await _branches.GetAsync(request.BranchId, cancellationToken) is null) return BadRequest(new { error = "Branch was not found." });
        if (request.Items is null || request.Items.Count == 0) return BadRequest(new { error = "At least one product is required." });

        IReadOnlyList<GoodsReceiptItemRequest> normalized;
        try
        {
            normalized = request.Items.GroupBy(x => x.ProductId)
                .Select(group => new GoodsReceiptItemRequest(group.Key, group.Sum(x => x.Quantity)))
                .OrderBy(x => x.ProductId)
                .ToList();
        }
        catch (OverflowException)
        {
            return BadRequest(new { error = "Receipt quantity is too large." });
        }
        if (normalized.Any(x => x.ProductId == Guid.Empty || x.Quantity <= 0))
            return BadRequest(new { error = "Receipt quantities must be positive whole units." });

        try
        {
            var result = await _unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                var receipt = new GoodsReceipt
                {
                    Id = Guid.NewGuid(),
                    ArtistId = request.ArtistId,
                    BranchId = request.BranchId,
                    Reference = string.IsNullOrWhiteSpace(request.Reference) ? null : request.Reference.Trim(),
                    CreatedAtUtc = DateTime.UtcNow
                };

                foreach (var item in normalized)
                {
                    var product = await _products.GetByIdAsync(item.ProductId, ct)
                        ?? throw new InvalidOperationException($"Product {item.ProductId} was not found.");
                    if (product.ArtistId != request.ArtistId)
                        throw new InvalidOperationException($"Product {product.Name} is not assigned to the selected artist.");

                    receipt.Items.Add(new GoodsReceiptLine
                    {
                        Id = Guid.NewGuid(),
                        GoodsReceiptId = receipt.Id,
                        ProductId = product.Id,
                        Quantity = item.Quantity,
                        PurchaseUnitPrice = product.PurchasePrice
                    });
                }

                _db.GoodsReceipts.Add(receipt);
                await _db.SaveChangesAsync(ct);
                foreach (var line in receipt.Items)
                    await _inventory.AdjustStockAsync(line.ProductId, receipt.BranchId, line.Quantity, $"Artist goods receipt {receipt.Id}", ct);

                return receipt;
            }, cancellationToken);

            return Ok(new { result.Id, result.ArtistId, result.BranchId, result.Reference, result.CreatedAtUtc, result.Items });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    public sealed record CreateGoodsReceiptRequest(Guid ArtistId, Guid BranchId, string? Reference, IReadOnlyList<GoodsReceiptItemRequest> Items);
    public sealed record GoodsReceiptItemRequest(Guid ProductId, int Quantity);
}
