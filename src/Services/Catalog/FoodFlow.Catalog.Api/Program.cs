using FoodFlow.Catalog.Api;
using FoodFlow.Catalog.Infrastructure;
using FoodFlow.ServiceDefaults;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddFoodFlowServiceDefaults("foodflow-catalog");
builder.Services.AddExceptionHandler<CatalogExceptionHandler>();
builder.AddFoodFlowFallbackExceptionHandler();
builder.AddCatalogInfrastructure();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    await db.Database.MigrateAsync();
}

app.MapFoodFlowServiceDefaults();
app.MapProductEndpoints();
app.Run();
