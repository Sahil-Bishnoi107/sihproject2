using BidGuard.Application.Common.Dtos;
using Mediator;

namespace BidGuard.Application.Features.Queries.Tenders.GetTenderById;

public sealed record GetTenderByIdQuery(Guid TenderId) : IQuery<TenderDto>;
