using BidGuard.Application.Common.Exceptions;
using BidGuard.Application.Common.Interfaces;
using BidGuard.Application.Services.Audit;
using BidGuard.Application.Services.Identity;
using BidGuard.Domain.Entities;
using BidGuard.Domain.Interfaces;
using Mediator;

namespace BidGuard.Application.Features.Commands.Users.CreateUser;

public sealed class CreateUserCommandHandler(
    IUserRepository users,
    IPasswordHasher hasher,
    IAuditService audit,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateUserCommand, UserDto>
{
    public async ValueTask<UserDto> Handle(CreateUserCommand command, CancellationToken ct)
    {
        if (await users.EmailExistsAsync(command.Email, ct))
            throw new ConflictException("An account with this email already exists.");

        var user = new User(command.Email, hasher.Hash(command.Password), command.FullName, command.Role);
        await users.AddAsync(user, ct);
        await audit.LogAsync("UserCreated", nameof(User), user.Id.ToString(), new { user.Email, Role = user.Role.ToString() }, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return UserDto.From(user);
    }
}
