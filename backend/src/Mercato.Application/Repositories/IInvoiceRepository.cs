namespace Mercato.Application.Repositories;

public interface IInvoiceRepository
{
    Task AddAsync(object invoice);
    Task<object?> GetAsync(Guid id);
}
