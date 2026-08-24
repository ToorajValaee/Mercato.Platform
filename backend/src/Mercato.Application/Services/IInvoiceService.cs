using Mercato.Domain.Entities;

namespace Mercato.Application.Services;

public interface IInvoiceService
{
    Task<Invoice> CreateAsync(Invoice invoice);
}
