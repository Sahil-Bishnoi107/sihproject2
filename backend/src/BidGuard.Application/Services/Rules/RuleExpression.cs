using System.Text.Json;
using BidGuard.Domain.Enums;

namespace BidGuard.Application.Services.Rules;

internal abstract record RuleNode;

internal sealed record LeafNode(string Field, string Op, JsonElement? Value, string? Source) : RuleNode;

internal sealed record AllNode(IReadOnlyList<RuleNode> Children) : RuleNode;

internal sealed record AnyNode(IReadOnlyList<RuleNode> Children) : RuleNode;

internal sealed record NotNode(RuleNode Child) : RuleNode;

public sealed class RuleExpressionException(string message) : Exception(message);

/// <summary>
/// Parser for the Compliance-as-Code JSON grammar:
/// leaf <c>{"field","op","value"?,"source"?}</c> or combinators <c>{"all":[..]}</c>, <c>{"any":[..]}</c>, <c>{"not":{..}}</c>.
/// </summary>
public static class RuleExpression
{
    public static readonly IReadOnlySet<string> Operators = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "exists", "==", "!=", ">=", "<=", ">", "<", "in", "before", "after", "verified",
    };

    private static readonly HashSet<string> OperatorsWithoutValue = new(StringComparer.OrdinalIgnoreCase) { "exists", "verified" };

    /// <summary>Returns an empty list when the expression is well-formed, otherwise the first problem found.</summary>
    public static IReadOnlyList<string> Validate(string json)
    {
        try
        {
            Parse(json);
            return [];
        }
        catch (Exception ex) when (ex is JsonException or RuleExpressionException)
        {
            return [ex.Message];
        }
    }

    internal static RuleNode Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return ParseNode(doc.RootElement);
    }

    private static RuleNode ParseNode(JsonElement e)
    {
        if (e.ValueKind != JsonValueKind.Object)
            throw new RuleExpressionException("Each rule node must be a JSON object.");

        if (e.TryGetProperty("all", out var all)) return new AllNode(ParseChildren(all, "all"));
        if (e.TryGetProperty("any", out var any)) return new AnyNode(ParseChildren(any, "any"));
        if (e.TryGetProperty("not", out var not)) return new NotNode(ParseNode(not));

        if (!e.TryGetProperty("field", out var field) || field.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(field.GetString()))
            throw new RuleExpressionException("Leaf node requires a non-empty 'field'.");
        if (!e.TryGetProperty("op", out var opEl) || opEl.ValueKind != JsonValueKind.String)
            throw new RuleExpressionException("Leaf node requires an 'op'.");

        var op = opEl.GetString()!.Trim().ToLowerInvariant();
        if (!Operators.Contains(op))
            throw new RuleExpressionException($"Unknown operator '{op}'.");

        JsonElement? value = e.TryGetProperty("value", out var v) ? v.Clone() : null;
        if (!OperatorsWithoutValue.Contains(op) && value is null)
            throw new RuleExpressionException($"Operator '{op}' requires a 'value'.");
        if (op == "in" && value?.ValueKind != JsonValueKind.Array)
            throw new RuleExpressionException("Operator 'in' requires an array 'value'.");

        string? source = e.TryGetProperty("source", out var s) && s.ValueKind == JsonValueKind.String ? s.GetString() : null;
        if (op == "verified" && (source is null || !Enum.TryParse<VerificationSource>(source, true, out _)))
            throw new RuleExpressionException($"Operator 'verified' requires 'source' to be one of: {string.Join(", ", Enum.GetNames<VerificationSource>())}.");

        return new LeafNode(field.GetString()!.Trim(), op, value, source?.ToUpperInvariant());
    }

    private static List<RuleNode> ParseChildren(JsonElement array, string name)
    {
        if (array.ValueKind != JsonValueKind.Array || array.GetArrayLength() == 0)
            throw new RuleExpressionException($"'{name}' requires a non-empty array.");
        return array.EnumerateArray().Select(ParseNode).ToList();
    }
}
