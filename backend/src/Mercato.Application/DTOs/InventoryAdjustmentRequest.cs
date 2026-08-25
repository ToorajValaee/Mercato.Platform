namespace Mercato.Application.DTOs;

public sealed class InventoryAdjustmentRequest
{
    public Guid ProductId { get; set; }
    public Guid BranchId { get; set; }
    public decimal Quantity { get; set; }
    public string Reason { get; set; } = string.Empty;
}
