namespace Mercato.Application.Services;

public class InvoiceProcessingService
{
    public bool ValidateInvoice(decimal totalAmount)
    {
        return totalAmount >= 0;
    }
}
