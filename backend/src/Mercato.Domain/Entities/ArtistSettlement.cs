namespace Mercato.Domain.Entities;

public class ArtistSettlement
{
    public Guid Id { get; set; }
    public Guid ArtistId { get; set; }
    public decimal TotalSalesCost { get; set; }
    public bool IsPaid { get; set; }
}
