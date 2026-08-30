namespace Mercato.Application.DTOs;

public record ProductDto(
    Guid Id,
    string Name,
    string? Sku,
    string? ImageUrl,
    decimal PurchasePrice,
    decimal SalePrice,
    Guid? CategoryId,
    Guid? ArtistId);
