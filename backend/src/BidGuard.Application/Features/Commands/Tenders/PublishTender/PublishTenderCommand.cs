using BidGuard.Application.Common.Dtos;
using Mediator;

namespace BidGuard.Application.Features.Commands.Tenders.PublishTender;

public sealed record PublishTenderCommand(Guid TenderId) : ICommand<TenderDto>;
