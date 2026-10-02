namespace BidGuard.Application.Common.Models;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);
