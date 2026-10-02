using System.Text.Json;
using BidGuard.Domain.Enums;

namespace BidGuard.Application.Services.Rules;

public interface IRuleEngine
{
    RuleResult Evaluate(string expressionJson, FactSet facts);
}

/// <summary>
/// Deterministic evaluator for Compliance-as-Code expressions. AI never decides outcomes; this does.
/// Combination semantics: <c>all</c> — any Fail ⇒ Fail, else any Missing ⇒ Missing, else Pass;
/// <c>any</c> — any Pass ⇒ Pass, else any Missing ⇒ Missing, else Fail; <c>not</c> swaps Pass/Fail and keeps Missing.
/// </summary>
public sealed class RuleEngine : IRuleEngine
{
    public RuleResult Evaluate(string expressionJson, FactSet facts)
    {
        RuleNode node;
        try
        {
            node = RuleExpression.Parse(expressionJson);
        }
        catch (Exception ex) when (ex is JsonException or RuleExpressionException)
        {
            return new RuleResult(RuleOutcome.Missing, $"Invalid rule expression: {ex.Message}", []);
        }
        return Eval(node, facts);
    }

    private static RuleResult Eval(RuleNode node, FactSet facts) => node switch
    {
        AllNode all => Combine(all.Children.Select(c => Eval(c, facts)).ToList(), requireAll: true),
        AnyNode any => Combine(any.Children.Select(c => Eval(c, facts)).ToList(), requireAll: false),
        NotNode not => Negate(Eval(not.Child, facts)),
        LeafNode leaf => EvalLeaf(leaf, facts),
        _ => throw new InvalidOperationException($"Unsupported node {node.GetType().Name}"),
    };

    private static RuleResult Combine(List<RuleResult> results, bool requireAll)
    {
        RuleOutcome outcome;
        if (requireAll)
            outcome = results.Any(r => r.Outcome == RuleOutcome.Fail) ? RuleOutcome.Fail
                : results.Any(r => r.Outcome == RuleOutcome.Missing) ? RuleOutcome.Missing
                : RuleOutcome.Pass;
        else
            outcome = results.Any(r => r.Outcome == RuleOutcome.Pass) ? RuleOutcome.Pass
                : results.Any(r => r.Outcome == RuleOutcome.Missing) ? RuleOutcome.Missing
                : RuleOutcome.Fail;

        return new RuleResult(outcome,
            string.Join("; ", results.Select(r => r.Explanation)),
            results.SelectMany(r => r.UsedKeys).Distinct().ToList());
    }

    private static RuleResult Negate(RuleResult r) => r.Outcome switch
    {
        RuleOutcome.Pass => r with { Outcome = RuleOutcome.Fail, Explanation = $"NOT ({r.Explanation})" },
        RuleOutcome.Fail => r with { Outcome = RuleOutcome.Pass, Explanation = $"NOT ({r.Explanation})" },
        _ => r,
    };

    private static RuleResult EvalLeaf(LeafNode leaf, FactSet facts)
    {
        var keys = new[] { leaf.Field };

        if (leaf.Op == "verified")
        {
            if (!facts.TryGetVerification(leaf.Source!, out var status))
                return new(RuleOutcome.Missing, $"{leaf.Field}: no {leaf.Source} registry check available", keys);
            return status == VerificationStatus.Verified
                ? new(RuleOutcome.Pass, $"{leaf.Field} verified against {leaf.Source} registry", keys)
                : new(RuleOutcome.Fail, $"{leaf.Field} {status} in {leaf.Source} registry", keys);
        }

        var hasValue = facts.TryGetValue(leaf.Field, out var actual) && !string.IsNullOrWhiteSpace(actual);

        if (leaf.Op == "exists")
            return hasValue
                ? new(RuleOutcome.Pass, $"{leaf.Field} present ({actual})", keys)
                : new(RuleOutcome.Fail, $"{leaf.Field} not found in submitted documents", keys);

        if (!hasValue)
            return new(RuleOutcome.Missing, $"{leaf.Field} not found in submitted documents", keys);

        if (leaf.Op == "in")
        {
            var options = new List<string>();
            foreach (var item in leaf.Value!.Value.EnumerateArray())
            {
                if (!TryResolve(item, facts, out var resolved, out var error))
                    return new(RuleOutcome.Missing, error, keys);
                options.Add(resolved);
            }
            var match = options.Any(o => ValuesEqual(actual, o));
            return new(match ? RuleOutcome.Pass : RuleOutcome.Fail,
                $"{leaf.Field} is {actual}; required one of [{string.Join(", ", options)}]", keys);
        }

        if (!TryResolve(leaf.Value!.Value, facts, out var expected, out var resolveError))
            return new(RuleOutcome.Missing, resolveError, keys);

        var described = $"{leaf.Field} is {actual}; required {leaf.Op} {expected}";

        bool? passed = leaf.Op switch
        {
            "==" => ValuesEqual(actual, expected),
            "!=" => !ValuesEqual(actual, expected),
            ">=" or "<=" or ">" or "<" => CompareNumbers(actual, expected, leaf.Op),
            "before" or "after" => CompareDates(actual, expected, leaf.Op),
            _ => null,
        };

        return passed switch
        {
            true => new(RuleOutcome.Pass, described, keys),
            false => new(RuleOutcome.Fail, described, keys),
            null => new(RuleOutcome.Missing, $"{described} (values not comparable)", keys),
        };
    }

    private static bool TryResolve(JsonElement value, FactSet facts, out string resolved, out string error)
    {
        error = string.Empty;
        resolved = value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? string.Empty,
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => value.GetRawText(),
        };

        if (value.ValueKind == JsonValueKind.String && resolved.StartsWith('$'))
        {
            var name = resolved[1..];
            if (!facts.TryGetVariable(name, out var variable))
            {
                error = $"Unknown tender variable ${name}";
                return false;
            }
            resolved = variable;
        }
        return true;
    }

    private static bool ValuesEqual(string a, string b)
    {
        if (ValueParsing.TryNumber(a, out var na) && ValueParsing.TryNumber(b, out var nb))
            return na == nb;
        return string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool? CompareNumbers(string actual, string expected, string op)
    {
        if (!ValueParsing.TryNumber(actual, out var a) || !ValueParsing.TryNumber(expected, out var e))
            return null;
        return op switch { ">=" => a >= e, "<=" => a <= e, ">" => a > e, "<" => a < e, _ => null };
    }

    private static bool? CompareDates(string actual, string expected, string op)
    {
        if (!ValueParsing.TryDate(actual, out var a) || !ValueParsing.TryDate(expected, out var e))
            return null;
        return op == "before" ? a < e : a > e;
    }
}
