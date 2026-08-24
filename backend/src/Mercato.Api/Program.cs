using Microsoft.AspNetCore.Mvc;
using Mercato.Application;
using Mercato.Infrastructure;
using Mercato.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<MercatoDbContext>();
    await DatabaseInitializer.InitializeAsync(dbContext);
}

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "Mercato.Api" }));
app.MapControllers();
app.Run();
