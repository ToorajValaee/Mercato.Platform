namespace Mercato.Domain.Entities;

public class StockMovement
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
    public string Type { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
