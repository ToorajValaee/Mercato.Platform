using Mercato.Domain.Entities;

namespace Mercato.Application.Repositories;

public interface IAccountingTransactionRepository
{
    Task<AccountingTransaction> AddAsync(
        AccountingTransaction transaction,
        CancellationToken cancellationToken = default);
}
