using BidGuard.Application.Services.Identity;
using Mediator;

namespace BidGuard.Application.Features.Commands.Auth.RefreshToken;

public sealed record RefreshTokenCommand(string RefreshToken) : ICommand<AuthResult>;
