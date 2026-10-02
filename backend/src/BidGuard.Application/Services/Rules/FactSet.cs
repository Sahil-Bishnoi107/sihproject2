using BidGuard.Domain.Enums;

namespace BidGuard.Application.Services.Rules;

/// <summary>
/// Everything the rule engine may look at for one bid: extracted/profile values, registry verification
/// outcomes keyed by source name (e.g. "GST"), and tender variables referenced as <c>$name</c>.
/// </summary>
public sealed record FactSet(
    IReadOnlyDictionary<string, string> Values,
    IReadOnlyDictionary<string, VerificationStatus> Verifications,
    IReadOnlyDictionary<string, string> Variables)
{
    public bool TryGetValue(string key, out string value) => TryGet(Values, key, out value!);

    public bool TryGetVerification(string source, out VerificationStatus status) => TryGet(Verifications, source, out status);

    public bool TryGetVariable(string name, out string value) => TryGet(Variables, name, out value!);

    private static bool TryGet<T>(IReadOnlyDictionary<string, T> dict, string key, out T value)
    {
        if (dict.TryGetValue(key, out value!)) return true;
        foreach (var (k, v) in dict)
        {
            if (string.Equals(k, key, StringComparison.OrdinalIgnoreCase))
            {
                value = v;
                return true;
            }
        }
        return false;
    }
}
