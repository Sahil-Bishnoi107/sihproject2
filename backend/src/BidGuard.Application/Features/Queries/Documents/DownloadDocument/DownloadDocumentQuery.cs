using BidGuard.Application.Common.Dtos;
using Mediator;

namespace BidGuard.Application.Features.Queries.Documents.DownloadDocument;

/// <summary>Stream a stored file, e.g. to open the page an evidence item points at.</summary>
public sealed record DownloadDocumentQuery(Guid DocumentId) : IQuery<DocumentContent>;
