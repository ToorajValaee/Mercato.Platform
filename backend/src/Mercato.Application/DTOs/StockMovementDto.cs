namespace Mercato.Application.DTOs;

public sealed record StockMovementDto(
    Guid Id,
    Guid BranchId,
    Guid ProductId,
    decimal Quantity,
    string Type,
    DateTime CreatedAtUtc);
