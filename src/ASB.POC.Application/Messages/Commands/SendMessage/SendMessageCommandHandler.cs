using ASB.POC.Application.Common.Interfaces;
using ASB.POC.Domain.Messaging;
using MediatR;

namespace ASB.POC.Application.Messages.Commands.SendMessage;

public sealed class SendMessageCommandHandler(IMessageBus messageBus)
    : IRequestHandler<SendMessageCommand, Guid>
{
    public async Task<Guid> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        var message = new ServiceBusMessage
        {
            Subject = request.Subject,
            Body = request.Body,
            Properties = request.Properties ?? new Dictionary<string, string>()
        };

        await messageBus.SendAsync(message, cancellationToken);

        return message.Id;
    }
}
