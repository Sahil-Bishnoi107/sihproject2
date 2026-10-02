using BidGuard.Application.Jobs;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BidGuard.Infrastructure.BackgroundJobs;

/// <summary>
/// When a job lands in Failed (retries exhausted) and its class implements <see cref="IHandlesPermanentFailure"/>,
/// run its clean-up in a fresh DI scope so the aggregate never stays stuck mid-pipeline.
/// </summary>
public sealed class PermanentFailureFilter(IServiceScopeFactory scopes, ILogger<PermanentFailureFilter> logger) : JobFilterAttribute, IApplyStateFilter
{
    public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
    {
        if (context.NewState is not FailedState failed) return;
        var job = context.BackgroundJob.Job;
        if (job is null || !typeof(IHandlesPermanentFailure).IsAssignableFrom(job.Type)) return;

        try
        {
            using var scope = scopes.CreateScope();
            var handler = (IHandlesPermanentFailure)scope.ServiceProvider.GetRequiredService(job.Type);
            var reason = failed.Exception?.Message ?? "unknown error";
            handler.OnPermanentFailureAsync(job.Args.ToList(), reason, CancellationToken.None).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Permanent-failure handler for job {JobId} ({JobType}) failed", context.BackgroundJob.Id, job.Type.Name);
        }
    }

    public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
    {
    }
}
