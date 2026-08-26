namespace Mercato.Domain.Entities;

public sealed class SalesReturn
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Guid BranchId { get; set; }
    public decimal TotalAmount { get; set; }
    public string RefundMethod { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<SalesReturnLine> Items { get; set; } = new List<SalesReturnLine>();
}

public sealed class SalesReturnLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SalesReturnId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
