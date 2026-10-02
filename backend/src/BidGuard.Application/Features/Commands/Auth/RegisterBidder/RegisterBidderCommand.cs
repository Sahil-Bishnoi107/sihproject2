using BidGuard.Application.Services.Identity;
using BidGuard.Domain.Enums;
using Mediator;

namespace BidGuard.Application.Features.Commands.Auth.RegisterBidder;

/// <summary>Self-registration for a bidding organization and its first user.</summary>
public sealed record RegisterBidderCommand(
    string Email,
    string Password,
    string FullName,
    string OrganizationName,
    string Pan,
    OrganizationCategory Category) : ICommand<AuthResult>;
