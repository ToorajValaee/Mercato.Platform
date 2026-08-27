using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Infrastructure;

namespace Mercato.InventorySync.Plugin;

public sealed class NopStartup : INopStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<INopInventoryGateway, NopInventoryGateway>();
        services.AddScoped<InventorySyncCore>();
        services.AddScoped<InventorySyncTask>();
    }

    public void Configure(IApplicationBuilder application)
    {
    }

    public int Order => 2;
}
