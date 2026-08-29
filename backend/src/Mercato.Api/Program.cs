using System.Text;
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
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));

    options.AddPolicy("UserAccess", policy =>
        policy.RequireRole("User", "Admin"));
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
                Role = "Admin"
            });
            await dbContext.SaveChangesAsync();
        }
    }
}

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "Mercato.Api" }));
app.MapGet("/", () => Results.Redirect("/pos/"));
app.MapControllers();
app.Run();
