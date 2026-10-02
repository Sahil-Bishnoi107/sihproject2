using BidGuard.Application.Services.Rules;
using BidGuard.Domain.Enums;
using FluentAssertions;

namespace BidGuard.Application.Tests.Services;

public class RuleEngineTests
{
    private readonly RuleEngine _engine = new();

    private static FactSet Facts(
        Dictionary<string, string>? values = null,
        Dictionary<string, VerificationStatus>? verifications = null,
        Dictionary<string, string>? variables = null) =>
        new(values ?? new(), verifications ?? new(), variables ?? new());

    [Theory]
    [InlineData(">=", "6000000", "5000000", RuleOutcome.Pass)]
    [InlineData(">=", "4000000", "5000000", RuleOutcome.Fail)]
    [InlineData("<=", "3", "5", RuleOutcome.Pass)]
    [InlineData(">", "5", "5", RuleOutcome.Fail)]
    [InlineData("<", "4", "5", RuleOutcome.Pass)]
    [InlineData("==", "5", "5.0", RuleOutcome.Pass)]
    [InlineData("!=", "5", "6", RuleOutcome.Pass)]
    public void Numeric_comparisons(string op, string actual, string expected, RuleOutcome outcome)
    {
        var result = _engine.Evaluate($$"""{"field":"turnover.avg3y","op":"{{op}}","value":{{expected}}}""",
            Facts(new() { ["turnover.avg3y"] = actual }));

        result.Outcome.Should().Be(outcome);
    }

    [Fact]
    public void Indian_digit_grouping_is_parsed_as_number()
    {
        var result = _engine.Evaluate("""{"field":"turnover.avg3y","op":">=","value":5000000}""",
            Facts(new() { ["turnover.avg3y"] = "50,00,000" }));

        result.Outcome.Should().Be(RuleOutcome.Pass);
    }

    [Fact]
    public void Missing_field_yields_Missing_not_exception()
    {
        var result = _engine.Evaluate("""{"field":"turnover.avg3y","op":">=","value":5000000}""", Facts());

        result.Outcome.Should().Be(RuleOutcome.Missing);
        result.Explanation.Should().Contain("turnover.avg3y");
    }

    [Fact]
    public void Exists_fails_when_field_absent()
    {
        _engine.Evaluate("""{"field":"gstin","op":"exists"}""", Facts()).Outcome.Should().Be(RuleOutcome.Fail);
        _engine.Evaluate("""{"field":"gstin","op":"exists"}""", Facts(new() { ["gstin"] = "27AAPFU0939F1ZV" }))
            .Outcome.Should().Be(RuleOutcome.Pass);
    }

    [Fact]
    public void String_equality_is_case_insensitive()
    {
        var result = _engine.Evaluate("""{"field":"oem.authorization","op":"==","value":"YES"}""",
            Facts(new() { ["oem.authorization"] = "yes" }));

        result.Outcome.Should().Be(RuleOutcome.Pass);
    }

    [Fact]
    public void In_matches_any_listed_value()
    {
        var expr = """{"field":"org.category","op":"in","value":["MSE","Startup"]}""";

        _engine.Evaluate(expr, Facts(new() { ["org.category"] = "mse" })).Outcome.Should().Be(RuleOutcome.Pass);
        _engine.Evaluate(expr, Facts(new() { ["org.category"] = "Large" })).Outcome.Should().Be(RuleOutcome.Fail);
    }

    [Fact]
    public void Verified_op_uses_verification_status()
    {
        var expr = """{"field":"gstin","op":"verified","source":"GST"}""";

        _engine.Evaluate(expr, Facts(verifications: new() { ["GST"] = VerificationStatus.Verified }))
            .Outcome.Should().Be(RuleOutcome.Pass);
        _engine.Evaluate(expr, Facts(verifications: new() { ["GST"] = VerificationStatus.Mismatch }))
            .Outcome.Should().Be(RuleOutcome.Fail);
        _engine.Evaluate(expr, Facts()).Outcome.Should().Be(RuleOutcome.Missing);
    }

    [Fact]
    public void After_resolves_variable_bidEndDate()
    {
        var expr = """{"field":"iso9001.expiry","op":"after","value":"$bidEndDate"}""";
        var vars = new Dictionary<string, string> { ["bidEndDate"] = "2026-10-15" };

        _engine.Evaluate(expr, Facts(new() { ["iso9001.expiry"] = "31/12/2027" }, variables: vars))
            .Outcome.Should().Be(RuleOutcome.Pass);
        _engine.Evaluate(expr, Facts(new() { ["iso9001.expiry"] = "2026-09-30" }, variables: vars))
            .Outcome.Should().Be(RuleOutcome.Fail);
    }

    [Fact]
    public void Unknown_variable_yields_Missing()
    {
        var result = _engine.Evaluate("""{"field":"iso9001.expiry","op":"after","value":"$nope"}""",
            Facts(new() { ["iso9001.expiry"] = "2027-01-01" }));

        result.Outcome.Should().Be(RuleOutcome.Missing);
    }

    [Fact]
    public void All_fail_dominates_missing()
    {
        var expr = """{"all":[{"field":"a","op":"exists"},{"field":"b","op":">=","value":1}]}""";

        _engine.Evaluate(expr, Facts()).Outcome.Should().Be(RuleOutcome.Fail);
        _engine.Evaluate(expr, Facts(new() { ["a"] = "x" })).Outcome.Should().Be(RuleOutcome.Missing);
        _engine.Evaluate(expr, Facts(new() { ["a"] = "x", ["b"] = "2" })).Outcome.Should().Be(RuleOutcome.Pass);
    }

    [Fact]
    public void Any_pass_dominates()
    {
        var expr = """{"any":[{"field":"a","op":">=","value":10},{"field":"b","op":"exists"}]}""";

        _engine.Evaluate(expr, Facts(new() { ["b"] = "y" })).Outcome.Should().Be(RuleOutcome.Pass);
        _engine.Evaluate(expr, Facts()).Outcome.Should().Be(RuleOutcome.Missing);
        _engine.Evaluate(expr, Facts(new() { ["a"] = "1" })).Outcome.Should().Be(RuleOutcome.Fail);
    }

    [Fact]
    public void Not_inverts_pass_and_fail_but_keeps_missing()
    {
        _engine.Evaluate("""{"not":{"field":"blacklisted","op":"exists"}}""", Facts())
            .Outcome.Should().Be(RuleOutcome.Pass);
        _engine.Evaluate("""{"not":{"field":"n","op":">=","value":1}}""", Facts())
            .Outcome.Should().Be(RuleOutcome.Missing);
    }

    [Fact]
    public void Explanation_includes_actual_and_expected_values()
    {
        var result = _engine.Evaluate("""{"field":"experience.years","op":">=","value":3}""",
            Facts(new() { ["experience.years"] = "2" }));

        result.Explanation.Should().Contain("2").And.Contain("3").And.Contain("experience.years");
        result.UsedKeys.Should().Contain("experience.years");
    }

    [Fact]
    public void Malformed_expression_yields_Missing_with_reason()
    {
        var result = _engine.Evaluate("""{"field":"a","op":"frobnicate"}""", Facts(new() { ["a"] = "1" }));

        result.Outcome.Should().Be(RuleOutcome.Missing);
        result.Explanation.Should().Contain("frobnicate");
    }

    [Theory]
    [InlineData("""{"field":"a","op":"exists"}""", true)]
    [InlineData("""{"all":[{"field":"a","op":">=","value":1}]}""", true)]
    [InlineData("""{"field":"a","op":"bogus"}""", false)]
    [InlineData("""{"field":"a","op":">="}""", false)]
    [InlineData("""{"field":"a","op":"verified"}""", false)]
    [InlineData("""not json""", false)]
    [InlineData("""{"all":[]}""", false)]
    public void Validate_reports_structural_errors(string json, bool valid)
    {
        RuleExpression.Validate(json).Should().HaveCount(valid ? 0 : 1);
    }
}
