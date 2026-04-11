using ASB.POC.API.Contracts;
using ASB.POC.Application.Messages.Commands.SendMessage;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ASB.POC.API.Controllers;

[ApiController]
[Route("api/servicebus")]
public sealed class ServiceBusController(IMediator mediator) : ControllerBase
{
    [HttpPost("messages")]
    [ProducesResponseType(typeof(SendMessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Send([FromBody] SendMessageRequest request, CancellationToken ct)
    {
        var messageId = await mediator.Send(
            new SendMessageCommand(request.Subject, request.Body, request.Properties), ct);

        return Ok(new SendMessageResponse(messageId));
    }

    [HttpPost("messages/batch")]
    [ProducesResponseType(typeof(BatchSendResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendBatch([FromBody] SendBatchRequest request, CancellationToken ct)
    {
        var ids = new List<Guid>();

        foreach (var item in request.Messages)
        {
            var id = await mediator.Send(
                new SendMessageCommand(item.Subject, item.Body, item.Properties), ct);
            ids.Add(id);
        }

        return Ok(new BatchSendResponse(ids));
    }
}
