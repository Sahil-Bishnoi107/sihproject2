using BidGuard.Application.Common.Interfaces;
using BidGuard.Application.Jobs;

namespace BidGuard.Application.Services.Bids;

/// <summary>Queues extract → verify → evaluate as Hangfire continuations; each step runs only if the previous succeeded.</summary>
public sealed class BidPipeline(IBackgroundJobScheduler jobs)
{
    public void Start(Guid bidId)
    {
        var extract = jobs.Enqueue<ExtractBidEntitiesJob>(j => j.RunAsync(bidId, CancellationToken.None));
        var verify = jobs.ContinueWith<VerifyBidCredentialsJob>(extract, j => j.RunAsync(bidId, CancellationToken.None));
        jobs.ContinueWith<EvaluateBidJob>(verify, j => j.RunAsync(bidId, CancellationToken.None));
    }
}
