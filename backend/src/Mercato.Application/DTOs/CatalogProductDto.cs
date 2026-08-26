namespace Mercato.Application.DTOs;

public sealed record CatalogProductDto(
    Guid ProductId,
    string Name,
    decimal SalePrice,
    Guid? CategoryId,
    Guid? ArtistId,
    Guid? BranchId,
    int? AvailableQuantity);
