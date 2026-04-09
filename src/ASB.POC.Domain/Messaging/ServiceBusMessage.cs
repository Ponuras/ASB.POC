namespace ASB.POC.Domain.Messaging;

public sealed class ServiceBusMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Subject { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public IReadOnlyDictionary<string, string> Properties { get; init; } = new Dictionary<string, string>();
}
