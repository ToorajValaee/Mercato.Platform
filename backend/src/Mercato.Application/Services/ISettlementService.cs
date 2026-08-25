namespace Mercato.Application.Services;

using Mercato.Domain.Entities;

public interface ISettlementService
{
    Task CalculateAsync(Guid artistId, DateTime from, DateTime to);

    Task<ArtistSettlement> CreateAsync(
        ArtistSettlement settlement,
        CancellationToken cancellationToken = default);
}
