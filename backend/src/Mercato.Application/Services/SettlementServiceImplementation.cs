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
        return RecordLineAsync(orderId, artistId, productId, quantity, purchaseUnitPrice, false, cancellationToken);
    }

    public Task RecordReturnAsync(
        Guid orderId,
        Guid artistId,
        Guid productId,
        int quantity,
        decimal purchaseUnitPrice,
        CancellationToken cancellationToken = default)
    {
        return RecordLineAsync(orderId, artistId, productId, quantity, purchaseUnitPrice, true, cancellationToken);
    }

    private Task RecordLineAsync(
        Guid orderId,
        Guid artistId,
        Guid productId,
        int quantity,
        decimal purchaseUnitPrice,
        bool isReturn,
        CancellationToken cancellationToken)
    {
        if (orderId == Guid.Empty || artistId == Guid.Empty || productId == Guid.Empty)
            throw new ArgumentException("Settlement references must be valid identifiers.");

        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        if (purchaseUnitPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(purchaseUnitPrice));

        var sign = isReturn ? -1 : 1;
        return _settlements.AddLineAsync(new SettlementLine
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ArtistId = artistId,
            ProductId = productId,
            QuantitySold = sign * quantity,
            PurchaseAmount = sign * purchaseUnitPrice * quantity
        }, cancellationToken);
    }

    public async Task<ArtistSettlement> CalculateAsync(
        Guid artistId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        if (artistId == Guid.Empty)
            throw new ArgumentException("Artist is required.", nameof(artistId));

        if (to <= from)
            throw new ArgumentException("Settlement end date must be after start date.", nameof(to));

        var existing = await _settlements.GetForPeriodAsync(
            artistId,
            from,
            to,
            cancellationToken);

        if (existing is not null)
            return existing;

        var lines = await _settlements.GetLinesAsync(
            artistId,
            from,
            to,
            cancellationToken);

        if (lines.Count == 0)
            throw new InvalidOperationException("No artist sales were found for the requested settlement period.");

        var settlement = new ArtistSettlement
        {
            Id = Guid.NewGuid(),
            ArtistId = artistId,
            PeriodFromUtc = from,
            PeriodToUtc = to,
            TotalSalesCost = lines.Sum(x => x.PurchaseAmount),
            IsPaid = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        return await _settlements.AddSettlementAsync(settlement, cancellationToken);
    }

    public Task<IReadOnlyList<ArtistSettlement>> GetSettlementsAsync(
        Guid? artistId = null,
        bool? isPaid = null,
        CancellationToken cancellationToken = default)
    {
        return _settlements.GetSettlementsAsync(artistId, isPaid, cancellationToken);
    }

    public Task<ArtistSettlement?> MarkPaidAsync(
        Guid settlementId,
        CancellationToken cancellationToken = default)
    {
        if (settlementId == Guid.Empty)
            throw new ArgumentException("Settlement is required.", nameof(settlementId));

        return _settlements.MarkPaidAsync(
            settlementId,
            DateTime.UtcNow,
            cancellationToken);
    }
}
