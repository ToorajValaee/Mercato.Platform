namespace Mercato.Domain.Entities;

public class ArtistSettlement
{
    public Guid Id { get; set; }
    public Guid ArtistId { get; set; }
    public DateTime PeriodFromUtc { get; set; }
    public DateTime PeriodToUtc { get; set; }
    public decimal TotalSalesCost { get; set; }
    public bool IsPaid { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAtUtc { get; set; }
}
