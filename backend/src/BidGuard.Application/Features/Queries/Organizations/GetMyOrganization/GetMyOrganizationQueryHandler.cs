using BidGuard.Application.Common.Dtos;
using BidGuard.Application.Common.Exceptions;
using BidGuard.Application.Common.Interfaces;
using BidGuard.Domain.Entities;
using BidGuard.Domain.Interfaces;
using Mediator;

namespace BidGuard.Application.Features.Queries.Organizations.GetMyOrganization;

public sealed class GetMyOrganizationQueryHandler(IOrganizationRepository organizations, ICurrentUserService currentUser)
    : IQueryHandler<GetMyOrganizationQuery, OrganizationDto>
{
    public async ValueTask<OrganizationDto> Handle(GetMyOrganizationQuery query, CancellationToken ct)
    {
        var orgId = currentUser.OrganizationId ?? throw new ForbiddenException("Only bidder accounts belong to an organization.");
        var org = await organizations.GetWithCredentialsAsync(orgId, ct) ?? throw new NotFoundException(nameof(Organization), orgId);
        return OrganizationDto.From(org);
    }
}
