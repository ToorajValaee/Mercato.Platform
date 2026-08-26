using Mercato.Domain.Entities;

namespace Mercato.Application.Repositories;

public interface IAccountingTransactionRepository
{
    Task<AccountingTransaction> AddAsync(
        AccountingTransaction transaction,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AccountingTransaction>> GetAsync(
        Guid? branchId = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        string? type = null,
        CancellationToken cancellationToken = default);
}
