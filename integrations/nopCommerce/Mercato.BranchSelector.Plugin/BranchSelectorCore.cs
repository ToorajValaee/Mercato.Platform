using Mercato.NopCommerce.Core;

namespace Mercato.BranchSelector.Plugin;

public sealed class BranchSelectorCore
{
    private readonly MercatoApiClient _mercato;

    public BranchSelectorCore(MercatoApiClient mercato)
    {
        _mercato = mercato;
    }

    public Task<IReadOnlyList<CatalogProduct>> GetAvailabilityAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        if (branchId == Guid.Empty) throw new ArgumentException("Branch is required.", nameof(branchId));
        return _mercato.GetCatalogAsync(branchId, cancellationToken);
    }
}
