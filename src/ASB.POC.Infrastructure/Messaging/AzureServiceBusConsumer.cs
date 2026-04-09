using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ASB.POC.Infrastructure.Messaging;

public sealed class AzureServiceBusConsumer : BackgroundService
{
    private readonly ServiceBusProcessor _processor;
    private readonly ILogger<AzureServiceBusConsumer> _logger;

    public AzureServiceBusConsumer(
        ServiceBusClient client,
        IOptions<ServiceBusOptions> options,
        ILogger<AzureServiceBusConsumer> logger)
    {
        _logger = logger;

        var opts = options.Value;
        _processor = string.IsNullOrEmpty(opts.SubscriptionName)
            ? client.CreateProcessor(opts.QueueOrTopicName)
            : client.CreateProcessor(opts.QueueOrTopicName, opts.SubscriptionName);

        _processor.ProcessMessageAsync += OnMessageReceived;
        _processor.ProcessErrorAsync += OnError;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Azure Service Bus consumer...");
        await _processor.StartProcessingAsync(stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken).ContinueWith(_ => Task.CompletedTask);

        _logger.LogInformation("Stopping Azure Service Bus consumer...");
        await _processor.StopProcessingAsync();
    }

    private Task OnMessageReceived(ProcessMessageEventArgs args)
    {
        var body = args.Message.Body.ToString();
        _logger.LogInformation(
            "Received message {MessageId} with subject '{Subject}': {Body}",
            args.Message.MessageId,
            args.Message.Subject,
            body);

        return args.CompleteMessageAsync(args.Message, args.CancellationToken);
    }

    private Task OnError(ProcessErrorEventArgs args)
    {
        _logger.LogError(
            args.Exception,
            "Error processing message. Source: {ErrorSource}, Entity: {EntityPath}",
            args.ErrorSource,
            args.EntityPath);

        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _processor.DisposeAsync().AsTask().GetAwaiter().GetResult();
        base.Dispose();
    }
}
