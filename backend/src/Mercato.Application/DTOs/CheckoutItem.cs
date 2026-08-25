namespace Mercato.Application.DTOs;

public sealed class CheckoutItem
{
    public Guid ProductId { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
}
