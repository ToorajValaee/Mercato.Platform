using Nop.Core;
using Nop.Services.Common;

namespace Mercato.BranchSelector.Plugin;

public sealed class BranchSelectionService
{
    public const string BranchAttribute = "Mercato.BranchId";

    private readonly IWorkContext _workContext;
    private readonly IGenericAttributeService _attributes;

    public BranchSelectionService(IWorkContext workContext, IGenericAttributeService attributes)
    {
        _workContext = workContext;
        _attributes = attributes;
    }

    public async Task<Guid?> GetSelectedBranchAsync(CancellationToken cancellationToken = default)
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        var value = await _attributes.GetAttributeAsync<string>(customer, BranchAttribute);
        return Guid.TryParse(value, out var branchId) && branchId != Guid.Empty ? branchId : null;
    }

    public async Task SelectBranchAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        if (branchId == Guid.Empty)
            throw new ArgumentException("Branch is required.", nameof(branchId));

        var customer = await _workContext.GetCurrentCustomerAsync();
        await _attributes.SaveAttributeAsync(customer, BranchAttribute, branchId.ToString("D"));
    }
}
