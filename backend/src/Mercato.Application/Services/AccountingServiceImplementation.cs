using Mercato.Application.DTOs;
using Mercato.Application.Repositories;
using Mercato.Domain.Entities;

namespace Mercato.Application.Services;

public sealed class AccountingServiceImplementation : IAccountingService
{
    private readonly IAccountingTransactionRepository _transactions;

    public AccountingServiceImplementation(IAccountingTransactionRepository transactions)
    {
        _transactions = transactions;
    }

    public Task<IReadOnlyList<AccountingTransaction>> GetTransactionsAsync(
        Guid? branchId = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        string? type = null,
        CancellationToken cancellationToken = default)
        => _transactions.GetAsync(branchId, fromUtc, toUtc, type, cancellationToken);

    public async Task<AccountingSummary> GetSummaryAsync(
        Guid? branchId = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        CancellationToken cancellationToken = default)
    {
        if (fromUtc.HasValue && toUtc.HasValue && toUtc <= fromUtc)
            throw new InvalidOperationException("Accounting report end date must be after start date.");

        var transactions = await _transactions.GetAsync(branchId, fromUtc, toUtc, null, cancellationToken);
        var grossSales = transactions.Where(x => x.Type == "Sale").Sum(x => x.Amount);
        var refunds = Math.Abs(transactions.Where(x => x.Type == "Refund").Sum(x => x.Amount));
        return new AccountingSummary(grossSales, refunds, grossSales - refunds, transactions.Count);
    }
}
