namespace Mercato.Application.DTOs;

public sealed record CatalogProductDto(
    Guid ProductId,
    string Name,
    string? Sku,
    string? ImageUrl,
    decimal SalePrice,
    Guid? CategoryId,
    string? CategoryName,
    Guid? ArtistId,
    Guid? BranchId,
    int? AvailableQuantity);
