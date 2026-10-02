using BidGuard.Application.Common.Dtos;
using Mediator;

namespace BidGuard.Application.Features.Commands.Tenders.UploadTenderDocument;

/// <summary>Attach the tender PDF and queue AI rule extraction.</summary>
public sealed record UploadTenderDocumentCommand(
    Guid TenderId,
    string FileName,
    string ContentType,
    Stream Content,
    long Length) : ICommand<TenderDto>;
