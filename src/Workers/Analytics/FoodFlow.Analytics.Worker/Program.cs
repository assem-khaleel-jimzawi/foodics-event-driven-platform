using FoodFlow.Analytics.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.UseUtcTimestamp = true;
    options.TimestampFormat = "O";
});

builder.Services.AddHostedService<PhasePlaceholderWorker>();

var host = builder.Build();
host.Run();
