using BidGuard.Application.Common.Interfaces;
using BidGuard.Domain.Interfaces;
using BidGuard.Domain.ValueObjects;
using Mediator;

namespace BidGuard.Application.Features.Queries.Dashboard.GetDashboardStats;

public sealed class GetDashboardStatsQueryHandler(IReportingRepository reporting, ICacheService cache)
    : IQueryHandler<GetDashboardStatsQuery, ComplianceStats>
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

    public async ValueTask<ComplianceStats> Handle(GetDashboardStatsQuery query, CancellationToken ct) =>
        await cache.GetOrSetAsync(CacheKeys.DashboardStats, () => reporting.GetComplianceStatsAsync(ct), Ttl, ct);
}
