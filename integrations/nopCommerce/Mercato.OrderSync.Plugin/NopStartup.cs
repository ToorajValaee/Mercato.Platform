using Mercato.NopCommerce.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nop.Core.Infrastructure;

namespace Mercato.OrderSync.Plugin;

public sealed class NopStartup : INopStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient("Mercato.OrderSync");
        services.TryAddScoped(sp =>
        {
            var options = new MercatoConnectorOptions(
                configuration["Mercato:BaseUrl"] ?? throw new InvalidOperationException("Mercato:BaseUrl is required."),
                configuration["Mercato:BearerToken"] ?? string.Empty);
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            return new MercatoApiClient(factory.CreateClient("Mercato.OrderSync"), options);
        });
        services.AddScoped<OrderSyncCore>();
    }

    public void Configure(IApplicationBuilder application) { }

    public int Order => 3020;
}
