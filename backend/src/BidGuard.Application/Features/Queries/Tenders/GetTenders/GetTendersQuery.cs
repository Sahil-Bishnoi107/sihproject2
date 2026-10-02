using BidGuard.Application.Common.Dtos;
using BidGuard.Application.Common.Models;
using BidGuard.Domain.Enums;
using Mediator;

namespace BidGuard.Application.Features.Queries.Tenders.GetTenders;

public sealed record GetTendersQuery(TenderStatus? Status, string? Search, int Page = 1, int PageSize = 20)
    : IQuery<PagedResult<TenderDto>>;
