namespace Mercato.Domain.Entities;

public class BranchTransfer
{
    public Guid Id { get; set; }
    public Guid SourceBranchId { get; set; }
    public Guid DestinationBranchId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
