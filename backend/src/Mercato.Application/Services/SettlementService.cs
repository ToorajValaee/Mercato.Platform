namespace Mercato.Application.Services;

public class SettlementService : ISettlementService
{
    public decimal CalculateArtistAmount(decimal purchasePrice, decimal soldQuantity)
    {
        return purchasePrice * soldQuantity;
    }
}
