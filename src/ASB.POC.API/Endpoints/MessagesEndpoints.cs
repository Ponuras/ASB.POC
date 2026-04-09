using ASB.POC.API.Contracts;
using ASB.POC.Application.Messages.Commands.SendMessage;
using MediatR;

namespace ASB.POC.API.Endpoints;

public static class MessagesEndpoints
{
    public static IEndpointRouteBuilder MapMessagesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/messages").WithTags("Messages");

        group.MapPost("/", async (SendMessageRequest request, IMediator mediator, CancellationToken ct) =>
        {
            var messageId = await mediator.Send(
                new SendMessageCommand(request.Subject, request.Body, request.Properties), ct);
            return Results.Ok(new SendMessageResponse(messageId));
        })
        .WithSummary("Send a message to Azure Service Bus")
        .WithDescription("Sends a single message to the configured queue or topic.");

        return app;
    }
}
