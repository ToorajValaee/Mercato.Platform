namespace Mercato.Domain.Entities;

public class Order
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public decimal TotalAmount { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
