using BidGuard.Application.Common.Dtos;
using Mediator;

namespace BidGuard.Application.Features.Queries.Organizations.GetMyOrganization;

/// <summary>The caller's organization profile with its credential wallet.</summary>
public sealed record GetMyOrganizationQuery : IQuery<OrganizationDto>;
