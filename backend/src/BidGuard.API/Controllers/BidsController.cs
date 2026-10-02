using BidGuard.Application.Common.Dtos;
using BidGuard.Application.Features.Commands.Bids.CreateBid;
using BidGuard.Application.Features.Commands.Bids.OverrideEvaluation;
using BidGuard.Application.Features.Commands.Bids.RecordDecision;
using BidGuard.Application.Features.Queries.Bids.GetBidById;
using BidGuard.Application.Features.Queries.Bids.GetBidReport;
using BidGuard.Application.Features.Queries.Bids.GetMyBids;
using BidGuard.Application.Features.Queries.Bids.GetTenderBids;
using BidGuard.Domain.Enums;
using BidGuard.Application.Features.Commands.Bids.SubmitBid;
using BidGuard.Application.Features.Commands.Bids.UploadBidDocument;
using BidGuard.Application.Services.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BidGuard.API.Controllers;

[Authorize]
[Route("api")]
public sealed class BidsController : ApiControllerBase
{
    /// <summary>Start an official bid on a published tender.</summary>
    [HttpPost("tenders/{tenderId:guid}/bids")]
    [Authorize(Policy = Policies.BidderOnly)]
    [ProducesResponseType<BidDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BidDto>> Create(Guid tenderId, CancellationToken ct) =>
        Ok(await Sender.Send(new CreateBidCommand(tenderId, IsSimulation: false), ct));

    /// <summary>Start a private pre-bid simulation: same checks, visible only to your organization.</summary>
    [HttpPost("tenders/{tenderId:guid}/simulate")]
    [Authorize(Policy = Policies.BidderOnly)]
    [ProducesResponseType<BidDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BidDto>> Simulate(Guid tenderId, CancellationToken ct) =>
        Ok(await Sender.Send(new CreateBidCommand(tenderId, IsSimulation: true), ct));

    [HttpPost("bids/{bidId:guid}/documents")]
    [Authorize(Policy = Policies.BidderOnly)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(UploadLimits.MaxBytes + 1024 * 1024)]
    [ProducesResponseType<DocumentDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DocumentDto>> UploadDocument(Guid bidId, IFormFile file, [FromForm] string? docType, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        return Ok(await Sender.Send(new UploadBidDocumentCommand(bidId, file.FileName, file.ContentType, stream, file.Length, docType), ct));
    }

    /// <summary>Submit for evaluation; extraction, verification and evaluation run in the background.</summary>
    [HttpPost("bids/{bidId:guid}/submit")]
    [Authorize(Policy = Policies.BidderOnly)]
    [ProducesResponseType<BidDto>(StatusCodes.Status202Accepted)]
    public async Task<ActionResult<BidDto>> Submit(Guid bidId, CancellationToken ct) =>
        Accepted(await Sender.Send(new SubmitBidCommand(bidId), ct));

    /// <summary>Your organization's bids and simulations.</summary>
    [HttpGet("bids/mine")]
    [Authorize(Policy = Policies.BidderOnly)]
    [ProducesResponseType<IReadOnlyList<BidDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BidDto>>> Mine(CancellationToken ct) =>
        Ok(await Sender.Send(new GetMyBidsQuery(), ct));

    [HttpGet("bids/{bidId:guid}")]
    [ProducesResponseType<BidDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BidDto>> Get(Guid bidId, CancellationToken ct) =>
        Ok(await Sender.Send(new GetBidByIdQuery(bidId), ct));

    /// <summary>Explainable report: per-rule verdicts with evidence, registry checks and a ranked fix list.</summary>
    [HttpGet("bids/{bidId:guid}/report")]
    [ProducesResponseType<BidReportDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BidReportDto>> Report(Guid bidId, CancellationToken ct) =>
        Ok(await Sender.Send(new GetBidReportQuery(bidId), ct));

    [HttpGet("tenders/{tenderId:guid}/bids")]
    [Authorize(Policy = Policies.OfficerOrAdmin)]
    [ProducesResponseType<IReadOnlyList<BidDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BidDto>>> ForTender(Guid tenderId, CancellationToken ct) =>
        Ok(await Sender.Send(new GetTenderBidsQuery(tenderId), ct));

    [HttpPut("bids/{bidId:guid}/evaluations/{evaluationId:guid}/override")]
    [Authorize(Policy = Policies.OfficerOrAdmin)]
    [ProducesResponseType<BidDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BidDto>> Override(Guid bidId, Guid evaluationId, OverrideRequest request, CancellationToken ct) =>
        Ok(await Sender.Send(new OverrideEvaluationCommand(bidId, evaluationId, request.Outcome, request.Reason), ct));

    [HttpPost("bids/{bidId:guid}/decision")]
    [Authorize(Policy = Policies.OfficerOrAdmin)]
    [ProducesResponseType<BidDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BidDto>> Decide(Guid bidId, DecisionRequest request, CancellationToken ct) =>
        Ok(await Sender.Send(new RecordDecisionCommand(bidId, request.Decision, request.Remarks), ct));

    public sealed record OverrideRequest(RuleOutcome Outcome, string Reason);

    public sealed record DecisionRequest(BidDecision Decision, string? Remarks);
}
