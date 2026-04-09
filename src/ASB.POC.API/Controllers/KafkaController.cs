using ASB.POC.API.Contracts;
using ASB.POC.Application.Messages.Commands.SendMessage;
using ASB.POC.Infrastructure.Messaging.Kafka;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ASB.POC.API.Controllers;

[ApiController]
[Route("api/kafka")]
public sealed class KafkaController(IMediator mediator, KafkaTopicService topicService) : ControllerBase
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

    [HttpDelete("topic")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteTopic(
        [FromQuery] string? groupId,
        [FromQuery] bool force = false,
        CancellationToken ct = default)
    {
        var result = await topicService.DeleteIfEmptyAsync(groupId, force, ct);

        return result switch
        {
            TopicDeleteResult.DeletedResult => Ok("Topic usunięty"),
            TopicDeleteResult.NotFoundResult => NotFound("Topic nie istnieje"),
            TopicDeleteResult.HasUnreadResult r => Conflict(
                $"Topic ma {r.UnreadCount} nieodebranych wiadomości — nie można usunąć"),
            _ => StatusCode(500)
        };
    }
}
