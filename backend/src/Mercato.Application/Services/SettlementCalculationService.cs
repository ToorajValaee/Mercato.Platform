namespace Mercato.Application.Services;

public class SettlementCalculationService
{
    public decimal CalculateArtistPayable(decimal purchasePrice, int quantity)
    {
        return purchasePrice * quantity;
    }
}
