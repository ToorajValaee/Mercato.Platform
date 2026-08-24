namespace Mercato.Application.DTOs;

public class ProductCreateRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public Guid? CategoryId { get; set; }
}
