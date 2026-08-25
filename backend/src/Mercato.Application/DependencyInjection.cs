using Microsoft.Extensions.DependencyInjection;

namespace Mercato.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<Services.IProductService, Services.ProductServiceImplementation>();
        services.AddScoped<Services.IInventoryService, Services.InventoryServiceImplementation>();
        services.AddScoped<Interfaces.IAuthService, Services.AuthService>();
        services.AddScoped<Services.PasswordService>();
        services.AddScoped<Services.IOrderService, Services.OrderServiceImplementation>();
        services.AddScoped<Services.IOrderCheckoutService, Services.OrderCheckoutServiceImplementation>();

        return services;
    }
}
