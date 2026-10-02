using System.Globalization;
using BidGuard.Application.Services.Rules;
using BidGuard.Domain.Entities;
using BidGuard.Domain.Enums;

namespace BidGuard.Application.Services.Bids;

/// <summary>
/// Assembles what the rule engine may look at for one bid: document-extracted values (highest confidence wins),
/// the organization profile under <c>org.*</c>, registry outcomes by source, and tender variables.
/// </summary>
public static class FactSetBuilder
{
    public static FactSet Build(Tender tender, Organization org, IEnumerable<ExtractedField> fields, IEnumerable<VerificationResult> verifications)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["org.legalName"] = org.LegalName,
            ["org.category"] = org.Category.ToString(),
            ["org.isStartup"] = org.IsRecognisedStartup ? "true" : "false",
            ["org.pan"] = org.Pan,
        };
        AddIfPresent(values, "org.gstin", org.Gstin);
        AddIfPresent(values, "org.udyam", org.UdyamNumber);
        AddIfPresent(values, "org.cin", org.Cin);
        AddIfPresent(values, "org.epfo", org.EpfoCode);
        AddIfPresent(values, "org.dpiit", org.DpiitNumber);

        foreach (var best in fields.GroupBy(f => f.Key, StringComparer.OrdinalIgnoreCase)
                     .Select(g => g.OrderByDescending(f => f.Confidence).First()))
            values[best.Key] = best.Value;

        if (!values.ContainsKey("turnover.avg3y") && ThreeYearAverage(values) is { } avg)
            values["turnover.avg3y"] = avg.ToString(CultureInfo.InvariantCulture);

        var verified = verifications
            .GroupBy(v => v.Source)
            .ToDictionary(g => g.Key.ToString(), g => g.OrderByDescending(v => v.CheckedAt).First().Status, StringComparer.OrdinalIgnoreCase);

        var variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["bidEndDate"] = tender.BidEndDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["estimatedValue"] = tender.EstimatedValue.ToString(CultureInfo.InvariantCulture),
            ["emdAmount"] = tender.EmdAmount.ToString(CultureInfo.InvariantCulture),
        };

        return new FactSet(values, verified, variables);
    }

    /// <summary>Average of the three most recent <c>turnover.fyYYYY</c> values, when at least three are present.</summary>
    private static decimal? ThreeYearAverage(Dictionary<string, string> values)
    {
        var years = values
            .Where(kv => kv.Key.StartsWith("turnover.fy", StringComparison.OrdinalIgnoreCase))
            .Select(kv => (Year: kv.Key[11..], Ok: ValueParsing.TryNumber(kv.Value, out var n), Amount: n))
            .Where(x => x.Ok)
            .OrderByDescending(x => x.Year, StringComparer.Ordinal)
            .Take(3)
            .ToList();
        return years.Count == 3 ? Math.Round(years.Average(x => x.Amount), 2) : null;
    }

    private static void AddIfPresent(Dictionary<string, string> values, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)) values[key] = value;
    }
}
