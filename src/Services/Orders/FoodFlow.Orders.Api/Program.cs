using FoodFlow.Orders.Api;
using FoodFlow.Orders.Infrastructure;
using FoodFlow.ServiceDefaults;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddFoodFlowServiceDefaults("foodflow-orders");
builder.Services.AddExceptionHandler<OrdersExceptionHandler>();
builder.AddFoodFlowFallbackExceptionHandler();
builder.AddOrdersInfrastructure();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
    await db.Database.MigrateAsync();
}

app.MapFoodFlowServiceDefaults();
app.MapOrderEndpoints();
app.Run();
