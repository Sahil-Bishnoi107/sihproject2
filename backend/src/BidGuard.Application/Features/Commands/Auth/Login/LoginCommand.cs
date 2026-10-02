using BidGuard.Application.Services.Identity;
using Mediator;

namespace BidGuard.Application.Features.Commands.Auth.Login;

public sealed record LoginCommand(string Email, string Password) : ICommand<AuthResult>;
