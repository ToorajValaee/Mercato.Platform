namespace Mercato.Application.Services;

public class PaymentProcessingService
{
    public bool IsValidPayment(decimal amount)
    {
        return amount > 0;
    }
}
