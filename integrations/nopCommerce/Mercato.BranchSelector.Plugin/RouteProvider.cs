using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace Mercato.BranchSelector.Plugin;

public sealed class RouteProvider : IRouteProvider
{
    public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapControllerRoute(
            name: BranchSelectorDefaults.SelectBranchRouteName,
            pattern: BranchSelectorDefaults.SelectBranchRoutePattern,
            defaults: new { controller = "BranchSelector", action = "Select" });
    }

    public int Priority => 0;
}
