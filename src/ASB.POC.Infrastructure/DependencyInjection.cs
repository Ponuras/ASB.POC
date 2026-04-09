using ASB.POC.Application.Common.Interfaces;
using ASB.POC.Infrastructure.Messaging;
using ASB.POC.Infrastructure.Messaging.Kafka;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ASB.POC.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var provider = configuration["MessageBus:Provider"] ?? "ServiceBus";

        if (provider.Equals("Kafka", StringComparison.OrdinalIgnoreCase))
            services.AddKafkaMessaging();
        else
            services.AddServiceBusMessaging();

        return services;
    }

    private static IServiceCollection AddServiceBusMessaging(this IServiceCollection services)
    {
        services
            .AddOptions<ServiceBusOptions>()
            .BindConfiguration(ServiceBusOptions.SectionName)
            .ValidateOnStart();

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ServiceBusOptions>>().Value;
            return new ServiceBusClient(options.ConnectionString);
        });

        services.AddSingleton<IMessageBus, AzureServiceBusSender>();

        return services;
    }

    private static IServiceCollection AddKafkaMessaging(this IServiceCollection services)
    {
        services.AddKafkaOptions();
        services.AddSingleton<IMessageBus, KafkaProducer>();
        services.AddScoped<KafkaTopicService>();

        return services;
    }

    public static IServiceCollection AddKafkaConsumer(this IServiceCollection services)
    {
        services.AddKafkaOptions();
        services.AddHostedService<KafkaConsumer>();

        return services;
    }

    private static IServiceCollection AddKafkaOptions(this IServiceCollection services)
    {
        services
            .AddOptions<KafkaOptions>()
            .BindConfiguration(KafkaOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}
