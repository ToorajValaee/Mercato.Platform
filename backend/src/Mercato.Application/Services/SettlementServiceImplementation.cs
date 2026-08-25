namespace Mercato.Application.Services;

public sealed class SettlementServiceImplementation : ISettlementService
{
    public Task CalculateAsync(Guid artistId, DateTime from, DateTime to)
    {
        return Task.CompletedTask;
    }
}
