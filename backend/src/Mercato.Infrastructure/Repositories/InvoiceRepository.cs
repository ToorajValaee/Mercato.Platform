namespace Mercato.Infrastructure.Repositories;

public class InvoiceRepository
{
    public Task SaveAsync(object invoice)
    {
        return Task.CompletedTask;
    }
}
