namespace Mercato.Application.DTOs;

public record UpdateProductRequest(
    string Name,
    string? Sku,
    decimal PurchasePrice,
    decimal SalePrice,
    Guid? CategoryId);
