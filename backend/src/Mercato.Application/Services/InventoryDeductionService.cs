namespace Mercato.Application.Services;

public class InventoryDeductionService
{
    public bool CanDeduct(int availableQuantity, int requestedQuantity)
    {
        return requestedQuantity > 0 && availableQuantity >= requestedQuantity;
    }
}
