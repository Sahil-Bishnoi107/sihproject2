using BidGuard.Application.Features.Queries.Documents.DownloadDocument;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BidGuard.API.Controllers;

[Authorize]
public sealed class DocumentsController : ApiControllerBase
{
    /// <summary>Download a tender or bid document you are allowed to see (evidence deep-links point here).</summary>
    [HttpGet("{id:guid}/download")]
    [ProducesResponseType<FileStreamResult>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        var file = await Sender.Send(new DownloadDocumentQuery(id), ct);
        return File(file.Content, file.ContentType, file.FileName);
    }
}
