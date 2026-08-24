using Microsoft.Extensions.DependencyInjection;
using Mercato.Infrastructure;

namespace Mercato.Api.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddMercatoServices(this IServiceCollection services)
    {
        services.AddMercatoInfrastructure();
        return services;
    }
}
