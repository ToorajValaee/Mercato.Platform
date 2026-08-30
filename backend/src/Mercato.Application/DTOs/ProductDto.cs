namespace Mercato.Application.DTOs;

public record ProductDto(
    Guid Id,
    string Name,
    string? Sku,
    decimal PurchasePrice,
    decimal SalePrice,
    Guid? CategoryId,
    Guid? ArtistId)
{
    public string? ImageUrl { get; init; }
}
