using ASB.POC.Domain.Messaging;

namespace ASB.POC.Application.Common.Interfaces;

public interface IMessageBus
{
    Task SendAsync(ServiceBusMessage message, CancellationToken cancellationToken = default);
    Task SendBatchAsync(IEnumerable<ServiceBusMessage> messages, CancellationToken cancellationToken = default);
}
