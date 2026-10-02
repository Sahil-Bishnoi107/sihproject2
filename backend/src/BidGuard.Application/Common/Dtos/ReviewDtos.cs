using System.Text.Json;
using BidGuard.Domain.Entities;
using BidGuard.Domain.Enums;

namespace BidGuard.Application.Common.Dtos;

public sealed record EvaluationDto(
    Guid Id,
    Guid RuleId,
    string RuleCode,
    string RuleTitle,
    RuleCategory Category,
    bool IsMandatory,
    decimal Weight,
    RuleOutcome Outcome,
    RuleOutcome EffectiveOutcome,
    string Explanation,
    JsonElement Evidence,
    RuleOutcome? OfficerOverride,
    string? OverrideReason,
    string? SourceClause,
    int? SourcePage)
{
    public static EvaluationDto From(RuleEvaluation e, ComplianceRule rule)
    {
        using var doc = JsonDocument.Parse(e.EvidenceJson);
        return new EvaluationDto(e.Id, rule.Id, rule.Code, rule.Title, rule.Category, rule.IsMandatory, rule.Weight, e.Outcome,
            e.EffectiveOutcome, e.Explanation, doc.RootElement.Clone(), e.OfficerOverride, e.OverrideReason, rule.SourceClause, rule.SourcePage);
    }
}

public sealed record VerificationDto(VerificationSource Source, string Identifier, VerificationStatus Status, string? RegisteredName, DateTime CheckedAt)
{
    public static VerificationDto From(VerificationResult v) => new(v.Source, v.Identifier, v.Status, v.RegisteredName, v.CheckedAt);
}

/// <summary>One step on the bidder's path to compliance; <see cref="ScoreGain"/> = points recovered if fixed.</summary>
public sealed record FixDto(string RuleCode, string RuleTitle, bool IsMandatory, RuleOutcome CurrentOutcome, decimal ScoreGain, string Action);

public sealed record BidReportDto(
    BidDto Bid,
    TenderDto Tender,
    IReadOnlyList<DocumentDto> Documents,
    IReadOnlyList<VerificationDto> Verifications,
    IReadOnlyList<EvaluationDto> Evaluations,
    IReadOnlyList<FixDto> Fixes);
