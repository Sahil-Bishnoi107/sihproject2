using BidGuard.Application.Services.Identity;
using BidGuard.Domain.Enums;
using Mediator;

namespace BidGuard.Application.Features.Commands.Users.CreateUser;

/// <summary>Admin-only: provision an Officer or Admin account (bidders self-register).</summary>
public sealed record CreateUserCommand(string Email, string Password, string FullName, UserRole Role) : ICommand<UserDto>;
