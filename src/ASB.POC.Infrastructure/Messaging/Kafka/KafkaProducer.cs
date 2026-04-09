using ASB.POC.Application.Common.Interfaces;
using ASB.POC.Domain.Messaging;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace ASB.POC.Infrastructure.Messaging.Kafka;

public sealed class KafkaProducer : IMessageBus, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly string _topic;
    private readonly ILogger<KafkaProducer> _logger;

    public KafkaProducer(IOptions<KafkaOptions> options, ILogger<KafkaProducer> logger)
    {
        _topic = options.Value.Topic;
        _logger = logger;

        var config = new ProducerConfig
        {
            BootstrapServers = options.Value.BootstrapServers
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task SendAsync(ServiceBusMessage message, CancellationToken cancellationToken = default)
    {
        var kafkaMessage = new Message<string, string>
        {
            Key = message.Id.ToString(),
            Value = JsonSerializer.Serialize(message),
            Headers = BuildHeaders(message)
        };

        _logger.LogInformation("Sending message {MessageId} with subject '{Subject}' to topic '{Topic}'",
            message.Id, message.Subject, _topic);

        var result = await _producer.ProduceAsync(_topic, kafkaMessage, cancellationToken);

        _logger.LogInformation("Message {MessageId} delivered to {TopicPartitionOffset}",
            message.Id, result.TopicPartitionOffset);
    }

    public async Task SendBatchAsync(IEnumerable<ServiceBusMessage> messages, CancellationToken cancellationToken = default)
    {
        var tasks = messages.Select(m => SendAsync(m, cancellationToken));
        await Task.WhenAll(tasks);
    }

    private static Headers BuildHeaders(ServiceBusMessage message)
    {
        var headers = new Headers
        {
            { "subject", System.Text.Encoding.UTF8.GetBytes(message.Subject) },
            { "content-type", System.Text.Encoding.UTF8.GetBytes("application/json") }
        };

        foreach (var (key, value) in message.Properties)
            headers.Add(key, System.Text.Encoding.UTF8.GetBytes(value));

        return headers;
    }

    public void Dispose() => _producer.Dispose();
}
