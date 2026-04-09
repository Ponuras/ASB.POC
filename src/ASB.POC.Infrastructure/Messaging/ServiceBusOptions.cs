namespace ASB.POC.Infrastructure.Messaging;

public sealed class ServiceBusOptions
{
    public const string SectionName = "ServiceBus";

    public string ConnectionString { get; init; } = string.Empty;
    public string QueueOrTopicName { get; init; } = string.Empty;
    public string? SubscriptionName { get; init; }
}
