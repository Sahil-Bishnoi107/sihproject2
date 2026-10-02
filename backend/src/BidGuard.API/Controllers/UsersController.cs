using BidGuard.Application.Features.Commands.Users.CreateUser;
using BidGuard.Application.Services.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BidGuard.API.Controllers;

[Authorize(Policy = Policies.AdminOnly)]
public sealed class UsersController : ApiControllerBase
{
    /// <summary>Provision an Officer or Admin account.</summary>
    [HttpPost]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> Create(CreateUserCommand command, CancellationToken ct) =>
        Ok(await Sender.Send(command, ct));
}
