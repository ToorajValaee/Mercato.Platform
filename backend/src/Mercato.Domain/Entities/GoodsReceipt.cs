namespace Mercato.Domain.Entities;

public sealed class GoodsReceipt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ArtistId { get; set; }
    public Guid BranchId { get; set; }
    public string? Reference { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<GoodsReceiptLine> Items { get; set; } = new List<GoodsReceiptLine>();
}

public sealed class GoodsReceiptLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GoodsReceiptId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal PurchaseUnitPrice { get; set; }
}
