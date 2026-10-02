using BidGuard.Domain.Enums;

namespace BidGuard.Application.Common.Interfaces;

public sealed record ExtractedRuleDto(
    string Code,
    string Title,
    RuleCategory Category,
    bool IsMandatory,
    decimal Weight,
    string ExpressionJson,
    string? SourceClause,
    int? SourcePage,
    ExemptionKind? ExemptionKind);

public sealed record ExtractedFieldDto(string Key, string Value, decimal Confidence, int? Page, double[]? BoundingBox);

public sealed record EntityExtractionResult(bool OcrUsed, IReadOnlyList<ExtractedFieldDto> Fields);

/// <summary>Port to the AI sidecar: OCR, clause-to-rule drafting and entity extraction.</summary>
public interface IDocumentIntelligenceService
{
    Task<IReadOnlyList<ExtractedRuleDto>> ExtractRulesAsync(Stream document, string fileName, CancellationToken ct = default);
    Task<EntityExtractionResult> ExtractEntitiesAsync(Stream document, string fileName, string? docType, CancellationToken ct = default);
}
