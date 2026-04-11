using ASB.POC.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        var provider = ctx.Configuration["MessageBus:Provider"] ?? "ServiceBus";

        if (provider.Equals("Kafka", StringComparison.OrdinalIgnoreCase))
            services.AddKafkaConsumer();
        else
            services.AddServiceBusConsumer();
    })
    .Build();

await host.RunAsync();
