using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BidGuard.Application.Common.Interfaces;
using BidGuard.Domain.Enums;

namespace BidGuard.Infrastructure.AI;

public sealed class SidecarSettings
{
    public const string SectionName = "Sidecar";

    public string BaseUrl { get; init; } = "http://localhost:8000/";
    public int TimeoutSeconds { get; init; } = 120;
}

public sealed class SidecarException(string message) : Exception(message);

/// <summary>HTTP adapter to the Python bidguard-ai sidecar (OCR, clause→rule drafting, entity extraction).</summary>
public sealed class SidecarDocumentIntelligenceService(HttpClient http) : IDocumentIntelligenceService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<ExtractedRuleDto>> ExtractRulesAsync(Stream document, string fileName, CancellationToken ct = default)
    {
        using var content = BuildForm(document, fileName, docType: null);
        var payload = await PostAsync<RulesResponse>("extract-rules", content, ct);

        return (payload.Rules ?? []).Select(r => new ExtractedRuleDto(
            r.Code ?? "RULE",
            r.Title ?? r.Code ?? "Untitled rule",
            Enum.TryParse<RuleCategory>(r.Category, true, out var cat) ? cat : RuleCategory.Eligibility,
            r.IsMandatory,
            r.Weight is > 0 ? r.Weight.Value : 1m,
            r.Expression.ValueKind == JsonValueKind.Object ? r.Expression.GetRawText() : """{"field":"unknown","op":"exists"}""",
            r.SourceClause,
            r.SourcePage,
            Enum.TryParse<ExemptionKind>(r.ExemptionKind, true, out var kind) ? kind : null)).ToList();
    }

    public async Task<EntityExtractionResult> ExtractEntitiesAsync(Stream document, string fileName, string? docType, CancellationToken ct = default)
    {
        using var content = BuildForm(document, fileName, docType);
        var payload = await PostAsync<EntitiesResponse>("extract-entities", content, ct);

        return new EntityExtractionResult(payload.OcrUsed,
            (payload.Fields ?? [])
                .Where(f => !string.IsNullOrWhiteSpace(f.Key) && !string.IsNullOrWhiteSpace(f.Value))
                .Select(f => new ExtractedFieldDto(f.Key!, f.Value!, f.Confidence ?? 0.5m, f.Page, f.Bbox))
                .ToList());
    }

    private static MultipartFormDataContent BuildForm(Stream document, string fileName, string? docType)
    {
        var form = new MultipartFormDataContent();
        var file = new StreamContent(document);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        form.Add(file, "file", fileName);
        if (!string.IsNullOrWhiteSpace(docType))
            form.Add(new StringContent(docType), "doc_type");
        return form;
    }

    private async Task<T> PostAsync<T>(string path, HttpContent content, CancellationToken ct)
    {
        using var response = await http.PostAsync(path, content, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new SidecarException($"AI sidecar {path} failed with {(int)response.StatusCode}: {body[..Math.Min(body.Length, 500)]}");
        }
        return await response.Content.ReadFromJsonAsync<T>(Json, ct)
               ?? throw new SidecarException($"AI sidecar {path} returned an empty body.");
    }

    private sealed record RulesResponse(List<RuleItem>? Rules);

    private sealed record RuleItem(string? Code, string? Title, string? Category, bool IsMandatory, decimal? Weight,
        string? SourceClause, int? SourcePage, string? ExemptionKind, JsonElement Expression);

    private sealed record EntitiesResponse(bool OcrUsed, List<FieldItem>? Fields);

    private sealed record FieldItem(string? Key, string? Value, decimal? Confidence, int? Page, double[]? Bbox);
}
