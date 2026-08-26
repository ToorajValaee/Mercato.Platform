namespace Mercato.Application.DTOs;

public sealed class ReturnRequest
{
    public Guid OrderId { get; init; }
    public string RefundMethod { get; init; } = string.Empty;
    public IReadOnlyList<ReturnItem> Items { get; init; } = Array.Empty<ReturnItem>();
}

public sealed record ReturnItem(Guid ProductId, int Quantity);

public sealed record ReturnResult(
    Guid ReturnId,
    Guid OrderId,
    Guid RefundPaymentId,
    decimal Total,
    string Reference,
    DateTime CreatedAtUtc);
