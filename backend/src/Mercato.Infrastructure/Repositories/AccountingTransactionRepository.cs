using Microsoft.EntityFrameworkCore;
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

    public async Task<AccountingTransaction> AddAsync(AccountingTransaction transaction, CancellationToken cancellationToken = default)
    {
        _context.AccountingTransactions.Add(transaction);
        await _context.SaveChangesAsync(cancellationToken);
        return transaction;
    }

    public async Task<IReadOnlyList<AccountingTransaction>> GetAsync(
        Guid? branchId = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        string? type = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.AccountingTransactions.AsNoTracking().AsQueryable();
        if (branchId is Guid branch && branch != Guid.Empty) query = query.Where(x => x.BranchId == branch);
        if (fromUtc.HasValue) query = query.Where(x => x.CreatedAtUtc >= fromUtc.Value);
        if (toUtc.HasValue) query = query.Where(x => x.CreatedAtUtc < toUtc.Value);
        if (!string.IsNullOrWhiteSpace(type)) query = query.Where(x => x.Type == type.Trim());
        return await query.OrderByDescending(x => x.CreatedAtUtc).ToListAsync(cancellationToken);
    }
}
