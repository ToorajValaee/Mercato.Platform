using System.Net;
using System.Text.Json;
using Mercato.NopCommerce.Core;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.Extensions.Logging;
using Nop.Web.Framework.Components;

namespace Mercato.BranchSelector.Plugin;

public sealed class BranchSelectorViewComponent : NopViewComponent
{
    private readonly MercatoApiClient _mercato;
    private readonly BranchSelectionService _selection;
    private readonly IAntiforgery _antiforgery;
    private readonly ILogger<BranchSelectorViewComponent> _logger;

    public BranchSelectorViewComponent(
        MercatoApiClient mercato,
        BranchSelectionService selection,
        IAntiforgery antiforgery,
        ILogger<BranchSelectorViewComponent> logger)
    {
        _mercato = mercato;
        _selection = selection;
        _antiforgery = antiforgery;
        _logger = logger;
    }

    public async Task<IViewComponentResult> InvokeAsync(string widgetZone, object additionalData)
    {
        if (!_mercato.IsConfigured)
            return Empty();

        IReadOnlyList<MercatoBranch> branches;
        try
        {
            branches = await _mercato.GetBranchesAsync(HttpContext.RequestAborted);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Mercato branch selector could not load branches; the selector will not render for this request.");
            return Empty();
        }
        catch (OperationCanceledException ex) when (!HttpContext.RequestAborted.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Mercato branch selector timed out while loading branches; the selector will not render for this request.");
            return Empty();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Mercato branch selector received an invalid branch response; the selector will not render for this request.");
            return Empty();
        }

        if (branches.Count == 0)
            return Empty();

        var selected = await _selection.GetSelectedBranchAsync();
        var returnUrl = HttpContext.Request.PathBase + HttpContext.Request.Path + HttpContext.Request.QueryString;
        var actionUrl = Url.RouteUrl(BranchSelectorDefaults.SelectBranchRouteName) ?? "/mercato/branch/select";
        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
        var tokenField = string.IsNullOrWhiteSpace(tokens.RequestToken)
            ? string.Empty
            : $"<input type=\"hidden\" name=\"{WebUtility.HtmlEncode(tokens.FormFieldName)}\" value=\"{WebUtility.HtmlEncode(tokens.RequestToken)}\" />";

        var options = string.Join(string.Empty, branches.Select(branch =>
        {
            var isSelected = selected == branch.Id ? " selected" : string.Empty;
            return $"<option value=\"{branch.Id:D}\"{isSelected}>{WebUtility.HtmlEncode(branch.Name)}</option>";
        }));

        var html = $"<form method=\"post\" action=\"{WebUtility.HtmlEncode(actionUrl)}\" class=\"mercato-branch-selector\">" +
                   tokenField +
                   $"<input type=\"hidden\" name=\"returnUrl\" value=\"{WebUtility.HtmlEncode(returnUrl)}\" />" +
                   "<label for=\"mercato-branch\">Branch</label>" +
                   $"<select id=\"mercato-branch\" name=\"branchId\" onchange=\"this.form.submit()\">{options}</select>" +
                   "</form>";

        return new HtmlContentViewComponentResult(new HtmlString(html));
    }

    private static IViewComponentResult Empty()
        => new HtmlContentViewComponentResult(HtmlString.Empty);
}
