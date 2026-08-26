using Mercato.Domain.Entities;

namespace Mercato.Application.Repositories;

public interface ISettlementRepository
{
    Task AddLineAsync(SettlementLine line, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SettlementLine>> GetLinesAsync(
        Guid artistId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);
}
