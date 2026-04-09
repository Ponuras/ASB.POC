namespace ASB.POC.API.Contracts;

public sealed record SendMessageRequest(
    string Subject,
    string Body,
    Dictionary<string, string>? Properties = null);

public sealed record SendBatchRequest(
    IReadOnlyList<SendMessageRequest> Messages);

public sealed record SendMessageResponse(Guid MessageId);

public sealed record BatchSendResponse(IReadOnlyList<Guid> MessageIds);

public sealed record SendKafkaMessageRequest(string Subject, string Body, Dictionary<string, string>? Properties = null);

public sealed record SendKafkaBatchRequest(IReadOnlyList<SendKafkaMessageRequest> Messages);
