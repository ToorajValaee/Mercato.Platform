namespace Mercato.Application.DTOs;

public sealed class CheckoutRequest
{
    public Guid BranchId { get; init; }
    public Guid CustomerId { get; init; }
    public IReadOnlyList<CheckoutItem> Items { get; init; } = Array.Empty<CheckoutItem>();
}
