namespace Mercato.Domain.Entities;

public class Product
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal SalePrice { get; set; }
}
