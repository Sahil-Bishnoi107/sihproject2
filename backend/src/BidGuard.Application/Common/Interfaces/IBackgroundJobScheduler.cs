using System.Linq.Expressions;

namespace BidGuard.Application.Common.Interfaces;

/// <summary>Background job port; Hangfire implements it in Infrastructure.</summary>
public interface IBackgroundJobScheduler
{
    string Enqueue<TJob>(Expression<Func<TJob, Task>> job);
    string ContinueWith<TJob>(string parentJobId, Expression<Func<TJob, Task>> job);
    void AddOrUpdateRecurring<TJob>(string recurringJobId, Expression<Func<TJob, Task>> job, string cronExpression);
}
