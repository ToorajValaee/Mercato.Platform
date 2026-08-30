using Mercato.Application.Repositories;
using Mercato.Domain.Entities;
using Mercato.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Infrastructure.Repositories;

public sealed class PaymentRepository : IPaymentRepository
{
    private readonly MercatoDbContext _context;

    public PaymentRepository(MercatoDbContext context)
    {
        _context = context;
    }

    public async Task<Payment> AddAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync(cancellationToken);
        return payment;
    }

    public Task<Payment?> GetSalePaymentByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
        => _context.Payments.AsNoTracking()
            .Where(x => x.OrderId == orderId && x.Type == "Payment")
            .OrderBy(x => x.PaidAt)
            .FirstOrDefaultAsync(cancellationToken);
}
