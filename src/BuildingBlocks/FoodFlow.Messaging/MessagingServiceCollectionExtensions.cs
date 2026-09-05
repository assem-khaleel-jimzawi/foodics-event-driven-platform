using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FoodFlow.Messaging;

public static class MessagingServiceCollectionExtensions
{
    public static IHostApplicationBuilder AddFoodFlowRabbitMq(
        this IHostApplicationBuilder builder,
        Action<IBusRegistrationConfigurator> configureConsumers)
    {
        builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
        builder.Services.Configure<OutboxOptions>(builder.Configuration.GetSection(OutboxOptions.SectionName));
        builder.Services.Configure<KafkaOptions>(builder.Configuration.GetSection(KafkaOptions.SectionName));

        builder.Services.AddSingleton<IKafkaProducer, KafkaProducerService>();
        builder.Services.AddHostedService<KafkaTopicProvisioner>();

        builder.Services.AddMassTransit(x =>
        {
            configureConsumers(x);
            x.SetKebabCaseEndpointNameFormatter();
            x.UsingRabbitMq((context, cfg) =>
            {
                var options = context.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
                cfg.Host(options.Host, options.Port, options.VirtualHost, host =>
                {
                    host.Username(options.Username);
                    host.Password(options.Password);
                });

                cfg.ConfigureJsonSerializerOptions(json =>
                {
                    json.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                    json.PropertyNameCaseInsensitive = true;
                    return json;
                });

                cfg.PrefetchCount = 16;
                cfg.UseMessageRetry(retry =>
                {
                    retry.Ignore<PermanentMessagingException>();
                    retry.Intervals(
                        TimeSpan.FromMilliseconds(200),
                        TimeSpan.FromMilliseconds(400),
                        TimeSpan.FromMilliseconds(800));
                });
                cfg.UseConsumeFilter(typeof(TelemetryConsumeFilter<>), context);

                cfg.ConfigureEndpoints(context);
            });
        });

        builder.Services.Configure<MassTransitHostOptions>(options =>
        {
            options.WaitUntilStarted = true;
            options.StartTimeout = TimeSpan.FromSeconds(30);
            options.StopTimeout = TimeSpan.FromSeconds(30);
        });

        return builder;
    }

    public static IServiceCollection AddOutboxProcessor<TContext>(this IServiceCollection services)
        where TContext : Microsoft.EntityFrameworkCore.DbContext, IOutboxDbContext
    {
        services.AddHostedService<OutboxProcessor<TContext>>();
        return services;
    }
}
