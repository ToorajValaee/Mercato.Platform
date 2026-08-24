namespace Mercato.Application.Services;

public interface ISettlementService
{
    Task CalculateAsync(Guid artistId, DateTime from, DateTime to);
}
