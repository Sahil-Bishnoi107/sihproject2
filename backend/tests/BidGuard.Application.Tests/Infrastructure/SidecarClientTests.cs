using System.Net;
using System.Text;
using BidGuard.Domain.Enums;
using BidGuard.Infrastructure.AI;
using FluentAssertions;

namespace BidGuard.Application.Tests.Infrastructure;

public class SidecarClientTests
{
    private sealed class StubHandler(HttpStatusCode status, string json) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            LastRequest = request;
            LastBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
            return new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        }
    }

    private static SidecarDocumentIntelligenceService Client(StubHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("http://sidecar:8000/") });

    [Fact]
    public async Task ExtractRules_maps_sidecar_payload_to_dtos()
    {
        var handler = new StubHandler(HttpStatusCode.OK, """
            {"rules":[{"code":"ELG-TURNOVER","title":"Minimum average turnover","category":"Financial","isMandatory":true,
              "weight":3,"sourceClause":"Average annual turnover of Rs 50 Lakh","sourcePage":4,
              "exemptionKind":"PriorExperienceTurnover","expression":{"field":"turnover.avg3y","op":">=","value":5000000}}]}
            """);

        var rules = await Client(handler).ExtractRulesAsync(new MemoryStream([1, 2, 3]), "tender.pdf");

        handler.LastRequest!.RequestUri!.AbsolutePath.Should().Be("/extract-rules");
        rules.Should().ContainSingle();
        var rule = rules[0];
        rule.Code.Should().Be("ELG-TURNOVER");
        rule.Category.Should().Be(RuleCategory.Financial);
        rule.ExemptionKind.Should().Be(ExemptionKind.PriorExperienceTurnover);
        rule.SourcePage.Should().Be(4);
        rule.ExpressionJson.Should().Contain("turnover.avg3y");
    }

    [Fact]
    public async Task Unknown_category_falls_back_to_Eligibility()
    {
        var handler = new StubHandler(HttpStatusCode.OK, """
            {"rules":[{"code":"X","title":"t","category":"Weird","isMandatory":false,"weight":1,"expression":{"field":"a","op":"exists"}}]}
            """);

        var rules = await Client(handler).ExtractRulesAsync(new MemoryStream([1]), "t.pdf");

        rules[0].Category.Should().Be(RuleCategory.Eligibility);
        rules[0].ExemptionKind.Should().BeNull();
    }

    [Fact]
    public async Task ExtractEntities_sends_doc_type_and_maps_fields()
    {
        var handler = new StubHandler(HttpStatusCode.OK, """
            {"ocrUsed":true,"fields":[{"key":"gstin","value":"27AAECS1234K1Z5","confidence":0.97,"page":1,"bbox":[10,20,110,40]}]}
            """);

        var result = await Client(handler).ExtractEntitiesAsync(new MemoryStream([1]), "gst.pdf", "gst_certificate");

        handler.LastRequest!.RequestUri!.AbsolutePath.Should().Be("/extract-entities");
        handler.LastBody.Should().Contain("gst_certificate");
        result.OcrUsed.Should().BeTrue();
        result.Fields.Should().ContainSingle(f => f.Key == "gstin" && f.Page == 1 && f.BoundingBox!.Length == 4);
    }

    [Fact]
    public async Task Sidecar_error_raises_exception_with_status()
    {
        var handler = new StubHandler(HttpStatusCode.InternalServerError, """{"detail":"boom"}""");

        var act = async () => await Client(handler).ExtractEntitiesAsync(new MemoryStream([1]), "a.pdf", null);

        await act.Should().ThrowAsync<SidecarException>().WithMessage("*500*");
    }
}
