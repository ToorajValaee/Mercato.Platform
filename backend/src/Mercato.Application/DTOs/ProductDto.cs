namespace Mercato.Application.DTOs;

public record ProductDto(Guid Id, string Name, decimal PurchasePrice, decimal SalePrice, Guid? CategoryId);
