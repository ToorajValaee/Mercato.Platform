using Microsoft.AspNetCore.Mvc;
using Mercato.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "Mercato.Api" }));
app.MapControllers();
app.Run();
