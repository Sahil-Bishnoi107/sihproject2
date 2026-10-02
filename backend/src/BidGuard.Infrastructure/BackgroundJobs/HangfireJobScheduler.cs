using System.Linq.Expressions;
using BidGuard.Application.Common.Interfaces;
using Hangfire;

namespace BidGuard.Infrastructure.BackgroundJobs;

/// <summary>Hangfire implementation of the Application's background job port.</summary>
public sealed class HangfireJobScheduler(IBackgroundJobClient client, IRecurringJobManager recurring) : IBackgroundJobScheduler
{
    public string Enqueue<TJob>(Expression<Func<TJob, Task>> job) => client.Enqueue(job);

    // Hangfire's default continuation runs only when the parent succeeded.
    public string ContinueWith<TJob>(string parentJobId, Expression<Func<TJob, Task>> job) =>
        client.ContinueJobWith(parentJobId, job);

    public void AddOrUpdateRecurring<TJob>(string recurringJobId, Expression<Func<TJob, Task>> job, string cronExpression) =>
        recurring.AddOrUpdate(recurringJobId, job, cronExpression, new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
}
