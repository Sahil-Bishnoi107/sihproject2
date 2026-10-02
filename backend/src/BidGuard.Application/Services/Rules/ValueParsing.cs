using System.Globalization;

namespace BidGuard.Application.Services.Rules;

/// <summary>Lenient parsing for values pulled out of Indian procurement documents.</summary>
internal static class ValueParsing
{
    private static readonly string[] DateFormats =
    [
        "yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy", "dd.MM.yyyy", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm:ssZ",
        "yyyy-MM-ddTHH:mm:ss.fffffffZ", "dd MMM yyyy", "d MMM yyyy", "dd MMMM yyyy", "d MMMM yyyy",
    ];

    public static bool TryNumber(string raw, out decimal value)
    {
        var cleaned = raw.Replace(",", "").Replace("₹", "").Replace("Rs.", "", StringComparison.OrdinalIgnoreCase)
            .Replace("Rs", "", StringComparison.OrdinalIgnoreCase).Replace("INR", "", StringComparison.OrdinalIgnoreCase).Trim();
        return decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }

    public static bool TryDate(string raw, out DateTime value)
    {
        var s = raw.Trim();
        if (DateTime.TryParseExact(s, DateFormats, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out value))
        {
            value = value.Date;
            return true;
        }
        if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out value))
        {
            value = value.Date;
            return true;
        }
        return false;
    }
}
