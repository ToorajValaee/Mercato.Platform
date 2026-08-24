namespace Mercato.Application.DTOs;

public class TransferCreateRequest
{
    public Guid SourceBranchId { get; set; }
    public Guid DestinationBranchId { get; set; }
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
}
