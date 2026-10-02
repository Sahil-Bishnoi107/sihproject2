using System.Text.Json;
using BidGuard.Domain.Entities;
using BidGuard.Domain.Enums;

namespace BidGuard.Application.Common.Dtos;

public sealed record TenderDto(
    Guid Id,
    string GemBidNumber,
    string Title,
    string Category,
    decimal EstimatedValue,
    decimal EmdAmount,
    DateTime BidEndDate,
    TenderStatus Status,
    Guid? TenderDocumentId,
    string? FailureReason,
    DateTime? PublishedAt,
    DateTime CreatedAt)
{
    public static TenderDto From(Tender t) => new(t.Id, t.GemBidNumber, t.Title, t.Category, t.EstimatedValue, t.EmdAmount,
        t.BidEndDate, t.Status, t.TenderDocumentId, t.FailureReason, t.PublishedAt, t.CreatedAt);
}

public sealed record RuleDto(
    Guid Id,
    Guid TenderId,
    string Code,
    string Title,
    RuleCategory Category,
    bool IsMandatory,
    decimal Weight,
    JsonElement Expression,
    string? SourceClause,
    int? SourcePage,
    ExemptionKind? ExemptionKind,
    RuleStatus Status)
{
    public static RuleDto From(ComplianceRule r)
    {
        using var doc = JsonDocument.Parse(r.Expression);
        return new RuleDto(r.Id, r.TenderId, r.Code, r.Title, r.Category, r.IsMandatory, r.Weight, doc.RootElement.Clone(),
            r.SourceClause, r.SourcePage, r.ExemptionKind, r.Status);
    }
}
