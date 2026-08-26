namespace Mercato.Application.DTOs;

public sealed class CheckoutResult
{
    public Guid OrderId { get; init; }
    public Guid InvoiceId { get; init; }
    public Guid PaymentId { get; init; }
    public decimal Total { get; init; }
    public string ReceiptReference { get; init; } = string.Empty;
    public string Status { get; init; } = "Completed";
}
