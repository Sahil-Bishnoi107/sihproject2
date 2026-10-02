using BidGuard.Application.Services.Identity;
using Mediator;

namespace BidGuard.Application.Features.Queries.Auth.GetCurrentUser;

public sealed record GetCurrentUserQuery : IQuery<UserDto>;
