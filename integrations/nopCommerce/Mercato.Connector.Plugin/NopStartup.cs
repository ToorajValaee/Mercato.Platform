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
        services.TryAddScoped(sp =>
        {
            var options = new MercatoConnectorOptions(
                configuration[MercatoNopDefaults.BaseUrlConfigurationKey]
                    ?? throw new InvalidOperationException($"{MercatoNopDefaults.BaseUrlConfigurationKey} is required."),
                configuration[MercatoNopDefaults.BearerTokenConfigurationKey] ?? string.Empty);
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
