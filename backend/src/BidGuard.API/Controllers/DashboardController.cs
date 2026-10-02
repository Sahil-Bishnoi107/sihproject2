using BidGuard.Application.Features.Queries.Dashboard.GetDashboardStats;
using BidGuard.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BidGuard.API.Controllers;

[Authorize(Policy = Policies.OfficerOrAdmin)]
public sealed class DashboardController : ApiControllerBase
{
    [HttpGet("stats")]
    [ProducesResponseType<ComplianceStats>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ComplianceStats>> Stats(CancellationToken ct) =>
        Ok(await Sender.Send(new GetDashboardStatsQuery(), ct));
}
