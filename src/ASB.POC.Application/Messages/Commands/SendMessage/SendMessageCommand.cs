using MediatR;

namespace ASB.POC.Application.Messages.Commands.SendMessage;

public sealed record SendMessageCommand(
    string Subject,
    string Body,
    Dictionary<string, string>? Properties = null) : IRequest<Guid>;
