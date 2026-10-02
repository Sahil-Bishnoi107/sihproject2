using BidGuard.Application.Common.Dtos;
using Mediator;

namespace BidGuard.Application.Features.Commands.Tenders.CreateTender;

public sealed record CreateTenderCommand(
    string GemBidNumber,
    string Title,
    string Category,
    decimal EstimatedValue,
    decimal EmdAmount,
    DateTime BidEndDate) : ICommand<TenderDto>;
