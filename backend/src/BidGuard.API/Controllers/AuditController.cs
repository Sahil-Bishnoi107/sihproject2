using BidGuard.Application.Common.Models;
using BidGuard.Application.Features.Queries.Audit.GetAuditLogs;
using BidGuard.Application.Features.Queries.Audit.VerifyAuditChain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BidGuard.API.Controllers;

[Authorize(Policy = Policies.OfficerOrAdmin)]
public sealed class AuditController : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<AuditLogDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> List([FromQuery] GetAuditLogsQuery query, CancellationToken ct) =>
        Ok(await Sender.Send(query, ct));

    /// <summary>Recomputes the SHA-256 hash chain and reports the first tampered entry, if any.</summary>
    [HttpGet("verify")]
    [ProducesResponseType<AuditChainStatus>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AuditChainStatus>> Verify(CancellationToken ct) =>
        Ok(await Sender.Send(new VerifyAuditChainQuery(), ct));
}
