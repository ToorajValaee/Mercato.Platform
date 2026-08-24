namespace Mercato.Domain.Entities;

public class SettlementLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ArtistId { get; set; }
    public Guid ProductId { get; set; }
    public int QuantitySold { get; set; }
    public decimal PurchaseAmount { get; set; }
}
