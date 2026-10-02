using BidGuard.Application.Common.Dtos;
using Mediator;

namespace BidGuard.Application.Features.Queries.Bids.GetBidReport;

/// <summary>Full explainable report: per-rule verdicts with evidence, registry checks, documents and the fix list.</summary>
public sealed record GetBidReportQuery(Guid BidId) : IQuery<BidReportDto>;
