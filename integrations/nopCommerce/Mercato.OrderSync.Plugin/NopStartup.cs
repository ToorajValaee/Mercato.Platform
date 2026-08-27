using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Infrastructure;

namespace Mercato.OrderSync.Plugin;

public sealed class NopStartup : INopStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<OrderSyncCore>();
    }

    public void Configure(IApplicationBuilder application)
    {
    }

    public int Order => 2;
}
