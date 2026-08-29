namespace Mercato.Application.DTOs;

public sealed record ReturnableOrderDto(
    Guid OrderId,
    Guid BranchId,
    DateTime CreatedAtUtc,
    decimal TotalAmount,
    IReadOnlyList<ReturnableOrderLineDto> Items);

public sealed record ReturnableOrderLineDto(
    Guid ProductId,
    int SoldQuantity,
    int ReturnedQuantity,
    int ReturnableQuantity,
    decimal UnitPrice,
    decimal LineTotal);
