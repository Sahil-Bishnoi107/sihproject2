using Mediator;

namespace BidGuard.Application.Features.Commands.Auth.Logout;

public sealed record LogoutCommand(string RefreshToken) : ICommand;
