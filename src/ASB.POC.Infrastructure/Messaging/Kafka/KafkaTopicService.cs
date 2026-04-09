using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ASB.POC.Infrastructure.Messaging.Kafka;

public sealed class KafkaTopicService(IOptions<KafkaOptions> options, ILogger<KafkaTopicService> logger)
{
    private readonly KafkaOptions _options = options.Value;

    public async Task<TopicDeleteResult> DeleteIfEmptyAsync(
        string? groupId = null,
        bool force = false,
        CancellationToken ct = default)
    {
        var effectiveGroupId = groupId ?? _options.GroupId;

        using var adminClient = new AdminClientBuilder(
            new AdminClientConfig { BootstrapServers = _options.BootstrapServers }).Build();

        var metadata = adminClient.GetMetadata(_options.Topic, TimeSpan.FromSeconds(5));
        var topicMetadata = metadata.Topics.FirstOrDefault(t => t.Topic == _options.Topic);

        if (topicMetadata is null)
            return TopicDeleteResult.NotFound;

        var partitions = topicMetadata.Partitions
            .Select(p => new TopicPartition(_options.Topic, p.PartitionId))
            .ToList();

        var unread = await CountUnreadMessagesAsync(adminClient, partitions, effectiveGroupId);

        if (unread > 0 && !force)
        {
            logger.LogWarning(
                "Nie można usunąć topicu '{Topic}' — {Unread} wiadomości nieodebranych przez grupę '{GroupId}'",
                _options.Topic, unread, effectiveGroupId);

            return TopicDeleteResult.HasUnreadMessages(unread);
        }

        if (unread > 0)
            logger.LogWarning(
                "Wymuszono usunięcie topicu '{Topic}' — {Unread} wiadomości nieodebranych (force=true)",
                _options.Topic, unread);

        await adminClient.DeleteTopicsAsync([_options.Topic]);

        logger.LogInformation("Topic '{Topic}' usunięty", _options.Topic);

        return TopicDeleteResult.Deleted;
    }

    private async Task<long> CountUnreadMessagesAsync(
        IAdminClient adminClient,
        List<TopicPartition> partitions,
        string groupId)
    {
        var groupOffsets = await adminClient.ListConsumerGroupOffsetsAsync(
        [
            new ConsumerGroupTopicPartitions(groupId, partitions)
        ]);

        var committedByPartition = groupOffsets[0]
            .Partitions
            .ToDictionary(p => p.Partition.Value, p => p.Offset.Value);

        using var consumer = new ConsumerBuilder<Ignore, Ignore>(new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = $"admin-check-{Guid.NewGuid()}"
        }).Build();

        long totalUnread = 0;

        foreach (var tp in partitions)
        {
            var watermarks = consumer.QueryWatermarkOffsets(tp, TimeSpan.FromSeconds(5));

            if (watermarks.High == 0)
                continue;

            var committed = committedByPartition.GetValueOrDefault(tp.Partition.Value, Offset.Unset.Value);
            var unread = watermarks.High - (committed == Offset.Unset.Value ? 0 : committed);

            logger.LogInformation(
                "Partycja [{Partition}]: high={High}, committed={Committed}, unread={Unread}, group={GroupId}",
                tp.Partition.Value, watermarks.High, committed, unread, groupId);

            totalUnread += unread;
        }

        return totalUnread;
    }
}

public abstract record TopicDeleteResult
{
    public static TopicDeleteResult Deleted { get; } = new DeletedResult();
    public static TopicDeleteResult NotFound { get; } = new NotFoundResult();
    public static TopicDeleteResult HasUnreadMessages(long count) => new HasUnreadResult(count);

    public sealed record DeletedResult : TopicDeleteResult;
    public sealed record NotFoundResult : TopicDeleteResult;
    public sealed record HasUnreadResult(long UnreadCount) : TopicDeleteResult;
}
