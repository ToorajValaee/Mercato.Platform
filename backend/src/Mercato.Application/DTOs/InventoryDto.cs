namespace Mercato.Application.DTOs;

public record InventoryDto(
    Guid ProductId,
    Guid BranchId,
    decimal Quantity
);
