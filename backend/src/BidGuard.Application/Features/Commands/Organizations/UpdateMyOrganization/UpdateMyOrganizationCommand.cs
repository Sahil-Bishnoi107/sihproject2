using BidGuard.Application.Common.Dtos;
using BidGuard.Domain.Enums;
using Mediator;

namespace BidGuard.Application.Features.Commands.Organizations.UpdateMyOrganization;

public sealed record UpdateMyOrganizationCommand(
    string LegalName,
    OrganizationCategory Category,
    string? Gstin,
    string? UdyamNumber,
    string? Cin,
    string? EpfoCode,
    bool IsStartup,
    string? DpiitNumber) : ICommand<OrganizationDto>;
