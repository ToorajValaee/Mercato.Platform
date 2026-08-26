using Mercato.Application.Repositories;
using Mercato.Domain.Entities;
using Mercato.Infrastructure.Data;

namespace Mercato.Infrastructure.Repositories;

public sealed class AccountingTransactionRepository : IAccountingTransactionRepository
{
    private readonly MercatoDbContext _context;

    public AccountingTransactionRepository(MercatoDbContext context)
    {
        _context = context;
    }

    public async Task<AccountingTransaction> AddAsync(
        AccountingTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        _context.AccountingTransactions.Add(transaction);
        await _context.SaveChangesAsync(cancellationToken);
        return transaction;
    }
}
