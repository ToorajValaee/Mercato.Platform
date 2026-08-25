using Mercato.Domain.Entities;

namespace Mercato.Application.Services;

public sealed class SettlementServiceImplementation : ISettlementService
{
    public Task CalculateAsync(Guid artistId, DateTime from, DateTime to)
    {
        return Task.CompletedTask;
    }

    public Task<ArtistSettlement> CreateAsync(
        ArtistSettlement settlement,
        CancellationToken cancellationToken = default)
    {
        settlement.Id = Guid.NewGuid();
        return Task.FromResult(settlement);
    }
}
