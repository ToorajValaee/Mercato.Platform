namespace Mercato.Domain.Entities;

public class Invoice
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid BranchId { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal SubtotalAmount { get; set; }
    public string? DiscountName { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
}
