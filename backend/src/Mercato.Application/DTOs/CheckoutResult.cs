namespace Mercato.Application.DTOs;

public sealed class CheckoutResult
{
    public Guid OrderId { get; init; }
    public Guid InvoiceId { get; init; }
    public Guid PaymentId { get; init; }
    public Guid BranchId { get; init; }
    public decimal Subtotal { get; init; }
    public Guid? DiscountId { get; init; }
    public string? DiscountName { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal Total { get; init; }
    public string PaymentMethod { get; init; } = string.Empty;
    public string ReceiptReference { get; init; } = string.Empty;
    public DateTime PaidAtUtc { get; init; }
    public IReadOnlyList<ReceiptLine> Items { get; init; } = Array.Empty<ReceiptLine>();
    public string Status { get; init; } = "Completed";
}
