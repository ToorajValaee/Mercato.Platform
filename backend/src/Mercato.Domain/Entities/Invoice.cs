namespace Mercato.Domain.Entities;

public class Invoice
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid BranchId { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal TotalAmount { get; set; }
}
