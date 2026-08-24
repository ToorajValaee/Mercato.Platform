namespace Mercato.Domain.Entities;

public class PhysicalProductItem
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid ArtistId { get; set; }
    public Guid? BranchId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public decimal PurchasePrice { get; set; }
}
