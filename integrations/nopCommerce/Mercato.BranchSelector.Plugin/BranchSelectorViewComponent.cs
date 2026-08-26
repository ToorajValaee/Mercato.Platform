using System.Net;
using Mercato.NopCommerce.Core;
using Microsoft.AspNetCore.Mvc;

namespace Mercato.BranchSelector.Plugin;

public sealed class BranchSelectorViewComponent : ViewComponent
{
    private readonly MercatoApiClient _mercato;
    private readonly BranchSelectionService _selection;

    public BranchSelectorViewComponent(MercatoApiClient mercato, BranchSelectionService selection)
    {
        _mercato = mercato;
        _selection = selection;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var branches = await _mercato.GetBranchesAsync();
        if (branches.Count == 0)
            return Content(string.Empty);

        var selected = await _selection.GetSelectedBranchAsync();
        var returnUrl = HttpContext.Request.PathBase + HttpContext.Request.Path + HttpContext.Request.QueryString;
        var options = string.Join(string.Empty, branches.Select(branch =>
        {
            var isSelected = selected == branch.Id ? " selected" : string.Empty;
            return $"<option value=\"{branch.Id:D}\"{isSelected}>{WebUtility.HtmlEncode(branch.Name)}</option>";
        }));

        var html = $"<form method=\"post\" action=\"/mercato/branch/select\" class=\"mercato-branch-selector\">" +
                   $"<input type=\"hidden\" name=\"returnUrl\" value=\"{WebUtility.HtmlEncode(returnUrl)}\" />" +
                   "<label for=\"mercato-branch\">Branch</label>" +
                   $"<select id=\"mercato-branch\" name=\"branchId\" onchange=\"this.form.submit()\">{options}</select>" +
                   "</form>";

        return Content(html, "text/html");
    }
}
