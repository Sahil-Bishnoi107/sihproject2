using BidGuard.Application.Common.Dtos;
using BidGuard.Application.Common.Exceptions;
using BidGuard.Application.Common.Interfaces;
using BidGuard.Application.Services.Audit;
using BidGuard.Domain.Entities;
using BidGuard.Domain.Interfaces;
using Mediator;

namespace BidGuard.Application.Features.Commands.Organizations.UpdateMyOrganization;

public sealed class UpdateMyOrganizationCommandHandler(
    IOrganizationRepository organizations,
    ICurrentUserService currentUser,
    IAuditService audit,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateMyOrganizationCommand, OrganizationDto>
{
    public async ValueTask<OrganizationDto> Handle(UpdateMyOrganizationCommand command, CancellationToken ct)
    {
        var orgId = currentUser.OrganizationId ?? throw new ForbiddenException("Only bidder accounts belong to an organization.");
        var org = await organizations.GetWithCredentialsAsync(orgId, ct) ?? throw new NotFoundException(nameof(Organization), orgId);

        org.UpdateProfile(command.LegalName, command.Category, command.Gstin, command.UdyamNumber, command.Cin, command.EpfoCode,
            command.IsStartup, command.DpiitNumber);
        await audit.LogAsync("OrganizationUpdated", nameof(Organization), org.Id.ToString(), command, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return OrganizationDto.From(org);
    }
}
