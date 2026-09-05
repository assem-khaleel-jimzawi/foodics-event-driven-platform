using FoodFlow.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddFoodFlowServiceDefaults("foodflow-payments");
builder.AddFoodFlowFallbackExceptionHandler();

var app = builder.Build();
app.MapFoodFlowServiceDefaults();
app.Run();
