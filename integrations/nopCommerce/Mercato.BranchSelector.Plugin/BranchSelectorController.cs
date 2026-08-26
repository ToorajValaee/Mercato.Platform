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
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Select(Guid branchId, string? returnUrl, CancellationToken cancellationToken)
    {
        try
        {
            await _selection.SelectBranchAsync(branchId, cancellationToken);
            return Redirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/");
        }
        catch (ArgumentException)
        {
            return BadRequest();
        }
    }
}
