using BidGuard.Domain.Entities;
using BidGuard.Domain.Enums;

namespace BidGuard.Application.Common.Dtos;

public sealed record BidDto(
    Guid Id,
    Guid TenderId,
    string? TenderTitle,
    string? GemBidNumber,
    Guid OrganizationId,
    string? OrganizationName,
    bool IsSimulation,
    BidStatus Status,
    DateTime? SubmittedAt,
    decimal? OverallScore,
    BidDecision? SuggestedDecision,
    BidDecision? Decision,
    string? DecisionRemarks,
    string? FailureReason,
    DateTime CreatedAt)
{
    // Tender/Organization are null when the navigation was not loaded (e.g. a bid just created).
    public static BidDto From(Bid b) => new(b.Id, b.TenderId, b.Tender?.Title, b.Tender?.GemBidNumber, b.OrganizationId,
        b.Organization?.LegalName, b.IsSimulation, b.Status,
        b.SubmittedAt, b.OverallScore, b.SuggestedDecision, b.Decision, b.DecisionRemarks, b.FailureReason, b.CreatedAt);
}

public sealed record DocumentDto(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    string Sha256,
    string? DocType,
    ExtractionStatus ExtractionStatus,
    bool OcrUsed,
    DateTime CreatedAt)
{
    public static DocumentDto From(Document d) =>
        new(d.Id, d.FileName, d.ContentType, d.SizeBytes, d.Sha256, d.DocType, d.ExtractionStatus, d.OcrUsed, d.CreatedAt);
}
