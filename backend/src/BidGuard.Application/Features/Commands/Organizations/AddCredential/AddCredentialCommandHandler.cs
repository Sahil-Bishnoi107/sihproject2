using BidGuard.Application.Common.Dtos;
using BidGuard.Application.Common.Exceptions;
using BidGuard.Application.Common.Interfaces;
using BidGuard.Application.Services.Audit;
using BidGuard.Domain.Entities;
using BidGuard.Domain.Interfaces;
using Mediator;

namespace BidGuard.Application.Features.Commands.Organizations.AddCredential;

public sealed class AddCredentialCommandHandler(
    IOrganizationRepository organizations,
    ICredentialRepository credentials,
    ICurrentUserService currentUser,
    IAuditService audit,
    IUnitOfWork unitOfWork) : ICommandHandler<AddCredentialCommand, CredentialDto>
{
    public async ValueTask<CredentialDto> Handle(AddCredentialCommand command, CancellationToken ct)
    {
        var orgId = currentUser.OrganizationId ?? throw new ForbiddenException("Only bidder accounts belong to an organization.");
        var org = await organizations.GetByIdAsync(orgId, ct) ?? throw new NotFoundException(nameof(Organization), orgId);

        var credential = org.AddCredential(command.Type, command.Number, command.IssuedOn, command.ExpiresOn);
        await credentials.AddAsync(credential, ct);
        await audit.LogAsync("CredentialAdded", nameof(Organization), org.Id.ToString(),
            new { Type = credential.Type.ToString(), credential.Number, credential.ExpiresOn }, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return CredentialDto.From(credential);
    }
}
