using Microsoft.AspNetCore.Mvc;

namespace Mercato.BranchSelector.Plugin;

[Route("mercato/branch")]
public sealed class BranchSelectorController : Controller
{
    private readonly BranchSelectionService _selection;

    public BranchSelectorController(BranchSelectionService selection)
    {
        _selection = selection;
    }

    [HttpPost("select")]
    public async Task<IActionResult> Select(Guid branchId, string? returnUrl)
    {
        await _selection.SelectBranchAsync(branchId);
        return Redirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/");
    }
}
