namespace Mercato.Application.DTOs;

public class SettlementDto
{
    public Guid Id { get; set; }
    public Guid ArtistId { get; set; }
    public decimal TotalPurchaseAmount { get; set; }
    public bool IsPaid { get; set; }
}
