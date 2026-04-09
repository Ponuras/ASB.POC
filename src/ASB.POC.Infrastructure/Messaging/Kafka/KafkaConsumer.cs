using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ASB.POC.Infrastructure.Messaging.Kafka;

public sealed class KafkaConsumer : BackgroundService
{
    private readonly KafkaOptions _options;
    private readonly ILogger<KafkaConsumer> _logger;

    public KafkaConsumer(IOptions<KafkaOptions> options, ILogger<KafkaConsumer> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await EnsureTopicExistsAsync();
        await Task.Run(() => ConsumeLoop(stoppingToken), stoppingToken);
    }

    private async Task EnsureTopicExistsAsync()
    {
        using var adminClient = new AdminClientBuilder(
            new AdminClientConfig { BootstrapServers = _options.BootstrapServers }).Build();

        try
        {
            await adminClient.CreateTopicsAsync(
            [
                new TopicSpecification
                {
                    Name = _options.Topic,
                    NumPartitions = 1,
                    ReplicationFactor = 1
                }
            ]);

            _logger.LogInformation("Kafka topic '{Topic}' created", _options.Topic);
        }
        catch (CreateTopicsException ex)
            when (ex.Results[0].Error.Code == ErrorCode.TopicAlreadyExists)
        {
            _logger.LogDebug("Kafka topic '{Topic}' already exists", _options.Topic);
        }
    }

    private void ConsumeLoop(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _options.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        var partition = new TopicPartition(_options.Topic, new Partition(0));

        using var consumer = new ConsumerBuilder<string, string>(config).Build();

        var committed = consumer.Committed([partition], TimeSpan.FromSeconds(5));
        var startOffset = committed[0].Offset == Offset.Unset
            ? Offset.Beginning
            : committed[0].Offset;

        consumer.Assign(new TopicPartitionOffset(partition, startOffset));

        _logger.LogInformation(
            "Kafka consumer uruchomiony. Topic='{Topic}', GroupId='{GroupId}', offset={Offset}",
            _options.Topic, _options.GroupId,
            startOffset == Offset.Beginning ? "Beginning" : startOffset.Value.ToString());

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var result = consumer.Consume(stoppingToken);

                _logger.LogInformation(
                    "Odebrano [{Offset}] Key={Key}: {Value}",
                    result.Offset.Value,
                    result.Message.Key,
                    result.Message.Value);

                consumer.Commit(result);
            }
        }
        catch (OperationCanceledException)
        {
            // graceful shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd konsumera Kafka");
        }
        finally
        {
            consumer.Close();
            _logger.LogInformation("Kafka consumer zatrzymany");
        }
    }
}
