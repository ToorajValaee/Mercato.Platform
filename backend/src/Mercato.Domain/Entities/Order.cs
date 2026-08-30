namespace Mercato.Domain.Entities;

public class Order
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public decimal SubtotalAmount { get; set; }
    public Guid? DiscountId { get; set; }
    public string? DiscountName { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
