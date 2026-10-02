using BidGuard.Application.Common.Dtos;
using BidGuard.Application.Features.Commands.Notifications.MarkNotificationRead;
using BidGuard.Application.Features.Queries.Notifications.GetNotifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BidGuard.API.Controllers;

[Authorize]
public sealed class NotificationsController : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<NotificationDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<NotificationDto>>> List([FromQuery] bool unreadOnly, CancellationToken ct) =>
        Ok(await Sender.Send(new GetNotificationsQuery(unreadOnly), ct));

    [HttpPost("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        await Sender.Send(new MarkNotificationReadCommand(id), ct);
        return NoContent();
    }
}
