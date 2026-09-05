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

if (app.Environment.IsDevelopment() || app.Configuration.GetValue("Database:MigrateOnStartup", false))
{
    await FoodFlowServiceDefaults.RetryStartupAsync(async () =>
    {
        await using var scope = app.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<CatalogDbContext>().Database.MigrateAsync();
    });
}

app.MapFoodFlowServiceDefaults();
app.MapProductEndpoints();
app.Run();
