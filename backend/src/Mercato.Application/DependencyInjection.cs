using Microsoft.Extensions.DependencyInjection;

namespace Mercato.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<Services.IProductService, Services.ProductServiceImplementation>();
        services.AddScoped<Services.IBranchService, Services.BranchServiceImplementation>();
        services.AddScoped<Services.IInventoryService, Services.InventoryServiceImplementation>();
        services.AddScoped<Services.IBranchTransferService, Services.BranchTransferServiceImplementation>();
        services.AddScoped<Interfaces.IAuthService, Services.AuthService>();
        services.AddScoped<Services.PasswordService>();
        services.AddScoped<Services.IOrderService, Services.OrderServiceImplementation>();
        services.AddScoped<Services.IOrderCheckoutService, Services.OrderCheckoutServiceImplementation>();
        services.AddScoped<Services.IInvoiceService, Services.InvoiceServiceImplementation>();
        services.AddScoped<Services.ISettlementService, Services.SettlementServiceImplementation>();

        return services;
    }
}
