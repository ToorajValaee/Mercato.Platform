namespace Mercato.Application.DTOs;

public class InvoiceCreateRequest
{
    public Guid BranchId { get; set; }
    public Guid? CustomerId { get; set; }
    public List<InvoiceLineRequest> Items { get; set; } = new();
}

public class InvoiceLineRequest
{
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
}
