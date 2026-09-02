using System.Text;
using Mercato.Api.Services;
using Mercato.Application;
using Mercato.Application.Services;
using Mercato.Domain.Entities;
using Mercato.Infrastructure;
using Mercato.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CurrentUserBranchAccess>();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var jwt = builder.Configuration.GetSection("Jwt");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwt["Key"] ?? string.Empty))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("UserAccess", policy => policy.RequireRole("User", "Admin"));
});

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<MercatoDbContext>();
    await DatabaseInitializer.InitializeAsync(dbContext);

    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var bootstrapEmail = configuration["BootstrapAdmin:Email"]?.Trim();
    var bootstrapPassword = configuration["BootstrapAdmin:Password"];
    if (!string.IsNullOrWhiteSpace(bootstrapEmail) && !string.IsNullOrWhiteSpace(bootstrapPassword))
    {
        var existing = await dbContext.Users.FirstOrDefaultAsync(user => user.Email == bootstrapEmail);
        if (existing is null)
        {
            var passwordService = scope.ServiceProvider.GetRequiredService<PasswordService>();
            dbContext.Users.Add(new User
            {
                Email = bootstrapEmail,
                PasswordHash = passwordService.Hash(bootstrapPassword),
                Role = "Admin",
                CanAccessBackOffice = true
            });
            await dbContext.SaveChangesAsync();
        }
    }

    if (configuration.GetValue<bool>("BootstrapDemoData:Enabled") &&
        !await dbContext.Branches.AnyAsync() &&
        !await dbContext.Products.AnyAsync())
    {
        var branchId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var products = new[]
        {
            new Product { Id = Guid.Parse("20000000-0000-0000-0000-000000000001"), Name = "Ceramic Cup", Sku = "DEMO-CUP", PurchasePrice = 5m, SalePrice = 12.50m },
            new Product { Id = Guid.Parse("20000000-0000-0000-0000-000000000002"), Name = "Canvas Tote", Sku = "DEMO-TOTE", PurchasePrice = 7m, SalePrice = 18m },
            new Product { Id = Guid.Parse("20000000-0000-0000-0000-000000000003"), Name = "Notebook", Sku = "DEMO-NOTE", PurchasePrice = 3m, SalePrice = 9.75m },
            new Product { Id = Guid.Parse("20000000-0000-0000-0000-000000000004"), Name = "Art Print", Sku = "DEMO-PRINT", PurchasePrice = 8m, SalePrice = 24m }
        };

        dbContext.Branches.Add(new Branch { Id = branchId, Name = "Demo Store", Address = "Local POS demo" });
        dbContext.Products.AddRange(products);
        dbContext.StockMovements.AddRange(products.Select((product, index) => new StockMovement
        {
            Id = Guid.NewGuid(), BranchId = branchId, ProductId = product.Id,
            Quantity = 12 + index * 3, Type = "Demo opening stock", CreatedAtUtc = DateTime.UtcNow
        }));
        await dbContext.SaveChangesAsync();
    }
}

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "Mercato.Api" }));
app.MapControllers();
app.Run();
