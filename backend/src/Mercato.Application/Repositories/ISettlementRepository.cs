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

    Task<ArtistSettlement?> GetForPeriodAsync(
        Guid artistId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    Task<ArtistSettlement?> GetSettlementAsync(
        Guid settlementId,
        CancellationToken cancellationToken = default);

    Task<ArtistSettlement> AddSettlementAsync(
        ArtistSettlement settlement,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ArtistSettlement>> GetSettlementsAsync(
        Guid? artistId = null,
        bool? isPaid = null,
        CancellationToken cancellationToken = default);

    Task<ArtistSettlement?> MarkPaidAsync(
        Guid settlementId,
        DateTime paidAtUtc,
        CancellationToken cancellationToken = default);
}
