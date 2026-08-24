namespace Mercato.Domain.Entities;

public class BranchInventoryDebt
{
    public Guid Id { get; set; }
    public Guid FromBranchId { get; set; }
    public Guid ToBranchId { get; set; }
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
}
