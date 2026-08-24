namespace Mercato.Application.Repositories;

public interface ISettlementRepository
{
    Task AddSettlementAsync(object settlement);
    Task<IEnumerable<object>> GetPendingAsync();
}
