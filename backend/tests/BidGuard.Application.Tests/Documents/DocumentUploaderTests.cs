using BidGuard.Application.Services.Documents;
using BidGuard.Application.Tests.TestSupport;
using BidGuard.Domain.Enums;
using FluentAssertions;

namespace BidGuard.Application.Tests.Documents;

public class DocumentUploaderTests : IDisposable
{
    private readonly TestHarness _h = new();

    [Theory]
    [InlineData("evidence.pdf", "text/html", "application/pdf")]
    [InlineData("scan.PNG", "application/octet-stream", "image/png")]
    [InlineData("photo.jpeg", "text/html", "image/jpeg")]
    public async Task Stored_content_type_comes_from_the_extension_not_the_client(string fileName, string claimed, string expected)
    {
        var uploader = new DocumentUploader(_h.Storage, _h.Documents);

        var document = await uploader.UploadAsync(DocumentOwnerType.Bid, Guid.NewGuid(), fileName, claimed,
            new MemoryStream([1, 2, 3]), null, Guid.NewGuid(), CancellationToken.None);

        document.ContentType.Should().Be(expected);
    }

    [Fact]
    public async Task Path_segments_are_stripped_from_the_file_name()
    {
        var uploader = new DocumentUploader(_h.Storage, _h.Documents);

        var document = await uploader.UploadAsync(DocumentOwnerType.Bid, Guid.NewGuid(), "../../etc/passwd.pdf", "application/pdf",
            new MemoryStream([1]), null, Guid.NewGuid(), CancellationToken.None);

        document.FileName.Should().Be("passwd.pdf");
        document.StorageKey.Should().NotContain("..");
    }

    public void Dispose() => _h.Dispose();
}
