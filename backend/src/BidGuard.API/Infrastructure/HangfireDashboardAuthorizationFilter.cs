using Hangfire.Dashboard;

namespace BidGuard.API.Infrastructure;

/// <summary>
/// Open in Development for the demo; otherwise only authenticated Admins (cookie/bearer forwarded by a gateway).
/// </summary>
public sealed class HangfireDashboardAuthorizationFilter(bool isDevelopment) : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        if (isDevelopment) return true;
        var user = context.GetHttpContext().User;
        return user.Identity?.IsAuthenticated == true && user.IsInRole("Admin");
    }
}
