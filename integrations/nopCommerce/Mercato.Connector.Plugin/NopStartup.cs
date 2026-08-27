using Mercato.NopCommerce.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nop.Core.Infrastructure;
using Nop.Web.Framework.Infrastructure.Extensions;

namespace Mercato.Connector.Plugin;

public sealed class NopStartup : INopStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient("Mercato.Connector").WithProxy();
        services.TryAddScoped<IMercatoConfiguration, NopMercatoConfiguration>();
        services.TryAddScoped(sp =>
        {
            var configurationProvider = sp.GetRequiredService<IMercatoConfiguration>();
            if (string.IsNullOrWhiteSpace(configurationProvider.BaseUrl))
                throw new InvalidOperationException("Mercato Base URL is not configured. Configure the Mercato Connector plugin or set Mercato:BaseUrl.");

            var options = new MercatoConnectorOptions(
                configurationProvider.BaseUrl,
                configurationProvider.BearerToken);
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            return new MercatoApiClient(factory.CreateClient("Mercato.Connector"), options);
        });
        services.AddScoped<ConnectorPluginCore>();
    }

    public void Configure(IApplicationBuilder application)
    {
    }

    public int Order => 1;
}
