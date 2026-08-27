using Mercato.NopCommerce.Core;
using Nop.Core;
using Nop.Services.Common;

namespace Mercato.BranchSelector.Plugin;

public sealed class BranchSelectionService
{
    private readonly IWorkContext _workContext;
    private readonly IGenericAttributeService _attributes;
    private readonly MercatoApiClient _mercato;

    public BranchSelectionService(
        IWorkContext workContext,
        IGenericAttributeService attributes,
        MercatoApiClient mercato)
    {
        _workContext = workContext;
        _attributes = attributes;
        _mercato = mercato;
    }

    public async Task<Guid?> GetSelectedBranchAsync(CancellationToken cancellationToken = default)
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        var value = await _attributes.GetAttributeAsync<string>(customer, MercatoNopDefaults.BranchIdAttribute);
        return Guid.TryParse(value, out var branchId) && branchId != Guid.Empty ? branchId : null;
    }

    public async Task SelectBranchAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        if (branchId == Guid.Empty)
            throw new ArgumentException("Branch is required.", nameof(branchId));

        var branches = await _mercato.GetBranchesAsync(cancellationToken);
        if (!branches.Any(x => x.Id == branchId))
            throw new ArgumentException("The selected Mercato branch does not exist.", nameof(branchId));

        var customer = await _workContext.GetCurrentCustomerAsync();
        await _attributes.SaveAttributeAsync(customer, MercatoNopDefaults.BranchIdAttribute, branchId.ToString("D"));
    }
}
