namespace Mercato.Application.DTOs;

public sealed class CheckoutResult
{
    public Guid OrderId { get; init; }
    public Guid InvoiceId { get; init; }
    public decimal Total { get; init; }
    public string Status { get; init; } = "Completed";
}
