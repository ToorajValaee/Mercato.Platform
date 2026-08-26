namespace Mercato.Domain.Entities;

public class AccountingTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Guid InvoiceId { get; set; }
    public Guid BranchId { get; set; }
    public decimal Amount { get; set; }
    public string Type { get; set; } = "Sale";
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
