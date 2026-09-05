using FoodFlow.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddFoodFlowServiceDefaults("foodflow-inventory");
builder.AddFoodFlowFallbackExceptionHandler();

var app = builder.Build();
app.MapFoodFlowServiceDefaults();
app.Run();
