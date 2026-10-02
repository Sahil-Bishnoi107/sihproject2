using BidGuard.Application.Common.Exceptions;
using BidGuard.Application.Common.Interfaces;
using BidGuard.Application.Services.Audit;
using BidGuard.Application.Services.Identity;
using BidGuard.Domain.Entities;
using BidGuard.Domain.Enums;
using BidGuard.Domain.Interfaces;
using Mediator;

namespace BidGuard.Application.Features.Commands.Auth.RegisterBidder;

public sealed class RegisterBidderCommandHandler(
    IUserRepository users,
    IOrganizationRepository organizations,
    IPasswordHasher hasher,
    AuthTokenIssuer issuer,
    IAuditService audit,
    IUnitOfWork unitOfWork) : ICommandHandler<RegisterBidderCommand, AuthResult>
{
    public async ValueTask<AuthResult> Handle(RegisterBidderCommand command, CancellationToken ct)
    {
        if (await users.EmailExistsAsync(command.Email, ct))
            throw new ConflictException("An account with this email already exists.");
        if (await organizations.PanExistsAsync(command.Pan, ct))
            throw new ConflictException("An organization with this PAN is already registered.");

        var organization = new Organization(command.OrganizationName, command.Pan, command.Category);
        var user = new User(command.Email, hasher.Hash(command.Password), command.FullName, UserRole.Bidder, organization.Id);

        await organizations.AddAsync(organization, ct);
        await users.AddAsync(user, ct);
        var result = await issuer.IssueAsync(user, ct);
        await audit.LogAsync("BidderRegistered", nameof(Organization), organization.Id.ToString(),
            new { user.Email, organization.LegalName }, ct);

        await unitOfWork.SaveChangesAsync(ct);
        return result;
    }
}
