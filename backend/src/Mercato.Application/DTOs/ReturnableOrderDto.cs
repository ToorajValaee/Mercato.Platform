namespace Mercato.Application.DTOs;

public sealed record ReturnableOrderDto(
    Guid OrderId,
    Guid BranchId,
    DateTime CreatedAtUtc,
    decimal TotalAmount,
    IReadOnlyList<ReturnableOrderLineDto> Items)
{
    public decimal SubtotalAmount { get; init; }
    public string? DiscountName { get; init; }
    public decimal DiscountAmount { get; init; }
}

public sealed record ReturnableOrderLineDto(
    Guid ProductId,
    int SoldQuantity,
    int ReturnedQuantity,
    int ReturnableQuantity,
    decimal UnitPrice,
    decimal LineTotal)
{
    public string ProductName { get; init; } = string.Empty;
    public int Quantity => ReturnableQuantity;
}
