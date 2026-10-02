using BidGuard.Application.Common.Dtos;
using Mediator;

namespace BidGuard.Application.Features.Commands.Bids.UploadBidDocument;

/// <param name="DocType">Optional hint for extraction, e.g. gst_certificate, udyam_certificate, financials, iso_certificate.</param>
public sealed record UploadBidDocumentCommand(
    Guid BidId,
    string FileName,
    string ContentType,
    Stream Content,
    long Length,
    string? DocType) : ICommand<DocumentDto>;
