using BidGuard.Application.Common.Interfaces;

namespace BidGuard.Application.Jobs;

/// <summary>Registers the application's recurring jobs; called once at startup.</summary>
public static class RecurringJobs
{
    public static void Register(IBackgroundJobScheduler jobs) =>
        jobs.AddOrUpdateRecurring<CredentialExpiryAlertJob>(CredentialExpiryAlertJob.RecurringId,
            job => job.RunAsync(CancellationToken.None), CredentialExpiryAlertJob.Cron);
}
