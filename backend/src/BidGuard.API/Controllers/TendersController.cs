using BidGuard.Application.Common.Dtos;
using BidGuard.Application.Common.Models;
using BidGuard.Application.Features.Commands.Rules.ApproveRules;
using BidGuard.Application.Features.Commands.Rules.CreateRule;
using BidGuard.Application.Features.Commands.Rules.DeleteRule;
using BidGuard.Application.Features.Commands.Rules.UpdateRule;
using BidGuard.Application.Features.Commands.Tenders.CreateTender;
using BidGuard.Application.Features.Commands.Tenders.PublishTender;
using BidGuard.Application.Features.Commands.Tenders.UploadTenderDocument;
using BidGuard.Application.Features.Queries.Tenders.GetTenderById;
using BidGuard.Application.Features.Queries.Tenders.GetTenderRules;
using BidGuard.Application.Features.Queries.Tenders.GetTenders;
using BidGuard.Application.Services.Documents;
using BidGuard.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BidGuard.API.Controllers;

[Authorize]
public sealed class TendersController : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<TenderDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<TenderDto>>> List([FromQuery] TenderStatus? status, [FromQuery] string? search,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        Ok(await Sender.Send(new GetTendersQuery(status, search, page, pageSize), ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<TenderDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TenderDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await Sender.Send(new GetTenderByIdQuery(id), ct));

    [HttpPost]
    [Authorize(Policy = Policies.OfficerOrAdmin)]
    [ProducesResponseType<TenderDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<TenderDto>> Create(CreateTenderCommand command, CancellationToken ct)
    {
        var tender = await Sender.Send(command, ct);
        return CreatedAtAction(nameof(Get), new { id = tender.Id }, tender);
    }

    /// <summary>Upload the tender PDF; AI rule extraction runs in the background.</summary>
    [HttpPost("{id:guid}/document")]
    [Authorize(Policy = Policies.OfficerOrAdmin)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(UploadLimits.MaxBytes + 1024 * 1024)]
    [ProducesResponseType<TenderDto>(StatusCodes.Status202Accepted)]
    public async Task<ActionResult<TenderDto>> UploadDocument(Guid id, IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        var tender = await Sender.Send(new UploadTenderDocumentCommand(id, file.FileName, file.ContentType, stream, file.Length), ct);
        return Accepted(tender);
    }

    [HttpPost("{id:guid}/publish")]
    [Authorize(Policy = Policies.OfficerOrAdmin)]
    [ProducesResponseType<TenderDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TenderDto>> Publish(Guid id, CancellationToken ct) =>
        Ok(await Sender.Send(new PublishTenderCommand(id), ct));

    [HttpGet("{id:guid}/rules")]
    [ProducesResponseType<IReadOnlyList<RuleDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RuleDto>>> Rules(Guid id, CancellationToken ct) =>
        Ok(await Sender.Send(new GetTenderRulesQuery(id), ct));

    [HttpPost("{id:guid}/rules")]
    [Authorize(Policy = Policies.OfficerOrAdmin)]
    [ProducesResponseType<RuleDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<RuleDto>> CreateRule(Guid id, CreateRuleCommand command, CancellationToken ct) =>
        Ok(await Sender.Send(command with { TenderId = id }, ct));

    [HttpPut("{id:guid}/rules/{ruleId:guid}")]
    [Authorize(Policy = Policies.OfficerOrAdmin)]
    [ProducesResponseType<RuleDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<RuleDto>> UpdateRule(Guid id, Guid ruleId, UpdateRuleCommand command, CancellationToken ct) =>
        Ok(await Sender.Send(command with { TenderId = id, RuleId = ruleId }, ct));

    [HttpDelete("{id:guid}/rules/{ruleId:guid}")]
    [Authorize(Policy = Policies.OfficerOrAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteRule(Guid id, Guid ruleId, CancellationToken ct)
    {
        await Sender.Send(new DeleteRuleCommand(id, ruleId), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/rules/{ruleId:guid}/approve")]
    [Authorize(Policy = Policies.OfficerOrAdmin)]
    [ProducesResponseType<IReadOnlyList<RuleDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RuleDto>>> ApproveRule(Guid id, Guid ruleId, CancellationToken ct) =>
        Ok(await Sender.Send(new ApproveRulesCommand(id, ruleId), ct));

    [HttpPost("{id:guid}/rules/approve-all")]
    [Authorize(Policy = Policies.OfficerOrAdmin)]
    [ProducesResponseType<IReadOnlyList<RuleDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RuleDto>>> ApproveAll(Guid id, CancellationToken ct) =>
        Ok(await Sender.Send(new ApproveRulesCommand(id, null), ct));
}
