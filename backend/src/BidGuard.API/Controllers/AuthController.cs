using BidGuard.Application.Features.Commands.Auth.Login;
using BidGuard.Application.Features.Commands.Auth.Logout;
using BidGuard.Application.Features.Commands.Auth.RefreshToken;
using BidGuard.Application.Features.Commands.Auth.RegisterBidder;
using BidGuard.Application.Features.Queries.Auth.GetCurrentUser;
using BidGuard.Application.Services.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BidGuard.API.Controllers;

public sealed class AuthController : ApiControllerBase
{
    /// <summary>Register a bidding organization and its first (Bidder) user.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting(Policies.AuthRateLimit)]
    [ProducesResponseType<AuthResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResult>> Register(RegisterBidderCommand command, CancellationToken ct) =>
        Ok(await Sender.Send(command, ct));

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(Policies.AuthRateLimit)]
    [ProducesResponseType<AuthResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResult>> Login(LoginCommand command, CancellationToken ct) =>
        Ok(await Sender.Send(command, ct));

    /// <summary>Exchange a refresh token for a new token pair (the old refresh token is revoked).</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(Policies.AuthRateLimit)]
    [ProducesResponseType<AuthResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResult>> Refresh(RefreshTokenCommand command, CancellationToken ct) =>
        Ok(await Sender.Send(command, ct));

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(LogoutCommand command, CancellationToken ct)
    {
        await Sender.Send(command, ct);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserDto>> Me(CancellationToken ct) =>
        Ok(await Sender.Send(new GetCurrentUserQuery(), ct));
}
