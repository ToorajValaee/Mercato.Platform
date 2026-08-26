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

    Task CalculateAsync(Guid artistId, DateTime from, DateTime to);

    Task<ArtistSettlement> CreateAsync(
        ArtistSettlement settlement,
        CancellationToken cancellationToken = default);
}
