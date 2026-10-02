using BidGuard.Application.Common.Exceptions;
using BidGuard.Application.Common.Interfaces;
using BidGuard.Application.Services.Identity;
using BidGuard.Domain.Interfaces;
using Mediator;

namespace BidGuard.Application.Features.Queries.Auth.GetCurrentUser;

public sealed class GetCurrentUserQueryHandler(IUserRepository users, ICurrentUserService currentUser)
    : IQueryHandler<GetCurrentUserQuery, UserDto>
{
    public async ValueTask<UserDto> Handle(GetCurrentUserQuery query, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException("Not authenticated.");
        var user = await users.GetByIdAsync(userId, ct) ?? throw new NotFoundException("User", userId);
        return UserDto.From(user);
    }
}
