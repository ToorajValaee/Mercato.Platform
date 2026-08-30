namespace Mercato.Application.DTOs;

public record CreateProductRequest(
    string Name,
    string? Sku,
    string? ImageUrl,
    decimal PurchasePrice,
    decimal SalePrice,
    Guid? CategoryId,
    Guid? ArtistId);
