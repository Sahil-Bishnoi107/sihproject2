using BidGuard.Application.Common.Dtos;
using BidGuard.Application.Features.Commands.Organizations.AddCredential;
using BidGuard.Application.Features.Commands.Organizations.UpdateMyOrganization;
using BidGuard.Application.Features.Queries.Organizations.GetMyOrganization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BidGuard.API.Controllers;

[Authorize(Policy = Policies.BidderOnly)]
public sealed class OrganizationsController : ApiControllerBase
{
    [HttpGet("me")]
    [ProducesResponseType<OrganizationDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<OrganizationDto>> Get(CancellationToken ct) =>
        Ok(await Sender.Send(new GetMyOrganizationQuery(), ct));

    [HttpPut("me")]
    [ProducesResponseType<OrganizationDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<OrganizationDto>> Update(UpdateMyOrganizationCommand command, CancellationToken ct) =>
        Ok(await Sender.Send(command, ct));

    /// <summary>Add a credential (GST, Udyam, ISO...) to the wallet; expiry is monitored daily.</summary>
    [HttpPost("me/credentials")]
    [ProducesResponseType<CredentialDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CredentialDto>> AddCredential(AddCredentialCommand command, CancellationToken ct) =>
        Ok(await Sender.Send(command, ct));
}
