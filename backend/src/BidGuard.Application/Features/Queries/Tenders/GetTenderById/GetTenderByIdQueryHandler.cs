using BidGuard.Application.Common.Dtos;
using BidGuard.Application.Common.Exceptions;
using BidGuard.Application.Common.Interfaces;
using BidGuard.Domain.Entities;
using BidGuard.Domain.Interfaces;
using Mediator;

namespace BidGuard.Application.Features.Queries.Tenders.GetTenderById;

public sealed class GetTenderByIdQueryHandler(ITenderRepository tenders, ICurrentUserService currentUser)
    : IQueryHandler<GetTenderByIdQuery, TenderDto>
{
    public async ValueTask<TenderDto> Handle(GetTenderByIdQuery query, CancellationToken ct)
    {
        var tender = await tenders.GetByIdAsync(query.TenderId, ct) ?? throw new NotFoundException(nameof(Tender), query.TenderId);
        TenderAccess.EnsureVisible(tender, currentUser);
        return TenderDto.From(tender);
    }
}
