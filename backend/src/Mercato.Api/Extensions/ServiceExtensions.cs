using Mercato.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mercato.Api.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddMercatoServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMercatoInfrastructure(configuration);
        return services;
    }
}
