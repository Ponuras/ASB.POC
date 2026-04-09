using ASB.POC.Infrastructure;
using Microsoft.Extensions.Hosting;


var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((_, services) => services.AddKafkaConsumer())
    .Build();

await host.RunAsync();
