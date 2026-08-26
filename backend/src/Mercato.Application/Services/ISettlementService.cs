namespace Mercato.Application.Services;

using Mercato.Domain.Entities;

public interface ISettlementService
{
    Task RecordSaleAsync(
        Guid orderId,
        Guid artistId,
        Guid productId,
        int quantity,
        decimal purchaseUnitPrice,
        CancellationToken cancellationToken = default);

    Task RecordReturnAsync(
        Guid orderId,
        Guid artistId,
        Guid productId,
        int quantity,
        decimal purchaseUnitPrice,
        CancellationToken cancellationToken = default);

    Task<ArtistSettlement> CalculateAsync(
        Guid artistId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ArtistSettlement>> GetSettlementsAsync(
        Guid? artistId = null,
        bool? isPaid = null,
        CancellationToken cancellationToken = default);

    Task<ArtistSettlement?> MarkPaidAsync(
        Guid settlementId,
        CancellationToken cancellationToken = default);
}
