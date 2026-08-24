namespace Mercato.Domain.Entities;

public enum InventoryTransactionType
{
    Receive,
    Transfer,
    Sale,
    Return,
    Damage,
    Adjustment
}

public class InventoryTransaction
{
    public Guid Id { get; set; }
    public Guid ProductItemId { get; set; }
    public Guid? FromBranchId { get; set; }
    public Guid? ToBranchId { get; set; }
    public InventoryTransactionType Type { get; set; }
    public DateTime CreatedAt { get; set; }
}
