using BidGuard.Domain.ValueObjects;
using Mediator;

namespace BidGuard.Application.Features.Queries.Dashboard.GetDashboardStats;

/// <summary>Officer analytics: tenders, bids by decision, most-failed rules (cached for 5 minutes).</summary>
public sealed record GetDashboardStatsQuery : IQuery<ComplianceStats>;
