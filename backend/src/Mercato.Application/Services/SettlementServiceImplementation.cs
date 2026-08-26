using Mercato.Application.Repositories;
using Mercato.Domain.Entities;

namespace Mercato.Application.Services;

public sealed class SettlementServiceImplementation : ISettlementService
{
    private readonly ISettlementRepository _settlements;

    public SettlementServiceImplementation(ISettlementRepository settlements)
    {
        _settlements = settlements;
    }

    public Task RecordSaleAsync(
        Guid orderId,
        Guid artistId,
        Guid productId,
        int quantity,
        decimal purchaseUnitPrice,
        CancellationToken cancellationToken = default)
    {
        if (orderId == Guid.Empty || artistId == Guid.Empty || productId == Guid.Empty)
            throw new ArgumentException("Settlement sale references must be valid identifiers.");

        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        if (purchaseUnitPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(purchaseUnitPrice));

        return _settlements.AddLineAsync(new SettlementLine
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ArtistId = artistId,
            ProductId = productId,
            QuantitySold = quantity,
            PurchaseAmount = purchaseUnitPrice * quantity
        }, cancellationToken);
    }

    public async Task CalculateAsync(Guid artistId, DateTime from, DateTime to)
    {
        if (artistId == Guid.Empty)
            throw new ArgumentException("Artist is required.", nameof(artistId));

        if (to <= from)
            throw new ArgumentException("Settlement end date must be after start date.", nameof(to));

        await _settlements.GetLinesAsync(artistId, from, to);
    }

    public Task<ArtistSettlement> CreateAsync(
        ArtistSettlement settlement,
        CancellationToken cancellationToken = default)
    {
        settlement.Id = settlement.Id == Guid.Empty ? Guid.NewGuid() : settlement.Id;
        return Task.FromResult(settlement);
    }
}
