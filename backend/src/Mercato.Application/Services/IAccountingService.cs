using Mercato.Application.DTOs;
using Mercato.Domain.Entities;

namespace Mercato.Application.Services;

public interface IAccountingService
{
    Task<IReadOnlyList<AccountingTransaction>> GetTransactionsAsync(
        Guid? branchId = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        string? type = null,
        CancellationToken cancellationToken = default);

    Task<AccountingSummary> GetSummaryAsync(
        Guid? branchId = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        CancellationToken cancellationToken = default);
}
