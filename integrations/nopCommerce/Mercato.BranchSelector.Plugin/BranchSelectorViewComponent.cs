using System.Net;
using Mercato.NopCommerce.Core;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Nop.Web.Framework.Components;

namespace Mercato.BranchSelector.Plugin;

public sealed class BranchSelectorViewComponent : NopViewComponent
{
    private readonly MercatoApiClient _mercato;
    private readonly BranchSelectionService _selection;
    private readonly IAntiforgery _antiforgery;

    public BranchSelectorViewComponent(
        MercatoApiClient mercato,
        BranchSelectionService selection,
        IAntiforgery antiforgery)
    {
        _mercato = mercato;
        _selection = selection;
        _antiforgery = antiforgery;
    }

    public async Task<IViewComponentResult> InvokeAsync(string widgetZone, object additionalData)
    {
        var branches = await _mercato.GetBranchesAsync();
        if (branches.Count == 0)
            return new HtmlContentViewComponentResult(HtmlString.Empty);

        var selected = await _selection.GetSelectedBranchAsync();
        var returnUrl = HttpContext.Request.PathBase + HttpContext.Request.Path + HttpContext.Request.QueryString;
        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
        var tokenField = string.IsNullOrWhiteSpace(tokens.RequestToken)
            ? string.Empty
            : $"<input type=\"hidden\" name=\"{WebUtility.HtmlEncode(tokens.FormFieldName)}\" value=\"{WebUtility.HtmlEncode(tokens.RequestToken)}\" />";

        var options = string.Join(string.Empty, branches.Select(branch =>
        {
            var isSelected = selected == branch.Id ? " selected" : string.Empty;
            return $"<option value=\"{branch.Id:D}\"{isSelected}>{WebUtility.HtmlEncode(branch.Name)}</option>";
        }));

        var html = $"<form method=\"post\" action=\"/mercato/branch/select\" class=\"mercato-branch-selector\">" +
                   tokenField +
                   $"<input type=\"hidden\" name=\"returnUrl\" value=\"{WebUtility.HtmlEncode(returnUrl)}\" />" +
                   "<label for=\"mercato-branch\">Branch</label>" +
                   $"<select id=\"mercato-branch\" name=\"branchId\" onchange=\"this.form.submit()\">{options}</select>" +
                   "</form>";

        return new HtmlContentViewComponentResult(new HtmlString(html));
    }
}
