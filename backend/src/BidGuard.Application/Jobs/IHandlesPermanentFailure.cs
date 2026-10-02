namespace BidGuard.Application.Jobs;

/// <summary>
/// Implemented by background jobs that must leave their aggregate in a clean state once all retries are
/// exhausted (e.g. a bid must not stay "Extracting" forever). Invoked by the job runner's failure filter.
/// </summary>
public interface IHandlesPermanentFailure
{
    Task OnPermanentFailureAsync(IReadOnlyList<object?> jobArguments, string reason, CancellationToken ct);
}
