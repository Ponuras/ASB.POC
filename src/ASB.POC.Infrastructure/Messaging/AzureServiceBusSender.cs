using ASB.POC.Application.Common.Interfaces;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using DomainMessage = ASB.POC.Domain.Messaging.ServiceBusMessage;

namespace ASB.POC.Infrastructure.Messaging;

public sealed class AzureServiceBusSender : IMessageBus, IAsyncDisposable
{
    private readonly ServiceBusSender _sender;
    private readonly ILogger<AzureServiceBusSender> _logger;

    public AzureServiceBusSender(
        ServiceBusClient client,
        IOptions<ServiceBusOptions> options,
        ILogger<AzureServiceBusSender> logger)
    {
        _sender = client.CreateSender(options.Value.QueueOrTopicName);
        _logger = logger;
    }

    public async Task SendAsync(DomainMessage message, CancellationToken cancellationToken = default)
    {
        var asbMessage = MapToAsbMessage(message);

        _logger.LogInformation("Sending message {MessageId} with subject '{Subject}'", message.Id, message.Subject);

        await _sender.SendMessageAsync(asbMessage, cancellationToken);

        _logger.LogInformation("Message {MessageId} sent successfully", message.Id);
    }

    public async Task SendBatchAsync(IEnumerable<DomainMessage> messages, CancellationToken cancellationToken = default)
    {
        using var batch = await _sender.CreateMessageBatchAsync(cancellationToken);

        foreach (var message in messages)
        {
            var asbMessage = MapToAsbMessage(message);
            if (!batch.TryAddMessage(asbMessage))
                throw new InvalidOperationException($"Message {message.Id} is too large to fit in the batch.");
        }

        _logger.LogInformation("Sending batch of {Count} messages", batch.Count);

        await _sender.SendMessagesAsync(batch, cancellationToken);

        _logger.LogInformation("Batch of {Count} messages sent successfully", batch.Count);
    }

    private static ServiceBusMessage MapToAsbMessage(DomainMessage message)
    {
        var asbMessage = new ServiceBusMessage(JsonSerializer.Serialize(message))
        {
            MessageId = message.Id.ToString(),
            Subject = message.Subject,
            ContentType = "application/json"
        };

        foreach (var (key, value) in message.Properties)
            asbMessage.ApplicationProperties[key] = value;

        return asbMessage;
    }

    public async ValueTask DisposeAsync() => await _sender.DisposeAsync();
}
