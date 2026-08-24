namespace Mercato.Domain.Entities;

public class InventoryBalance
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
}
