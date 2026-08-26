namespace Mercato.Application.DTOs;

public sealed class CheckoutRequest
{
    public Guid BranchId { get; init; }
    public Guid CustomerId { get; init; }
    public string PaymentMethod { get; init; } = string.Empty;
    public string IdempotencyKey { get; init; } = string.Empty;
    public IReadOnlyList<CheckoutItem> Items { get; init; } = Array.Empty<CheckoutItem>();
}
