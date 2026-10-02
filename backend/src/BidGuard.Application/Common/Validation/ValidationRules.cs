using System.Text.RegularExpressions;
using FluentValidation;

namespace BidGuard.Application.Common.Validation;

/// <summary>Reusable validators for Indian statutory identifiers and credentials.</summary>
public static partial class ValidationRules
{
    [GeneratedRegex("^[A-Z]{5}[0-9]{4}[A-Z]$")]
    public static partial Regex PanRegex();

    [GeneratedRegex("^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z][1-9A-Z]Z[0-9A-Z]$")]
    public static partial Regex GstinRegex();

    [GeneratedRegex("^UDYAM-[A-Z]{2}-[0-9]{2}-[0-9]{7}$")]
    public static partial Regex UdyamRegex();

    [GeneratedRegex("^[LU][0-9]{5}[A-Z]{2}[0-9]{4}[A-Z]{3}[0-9]{6}$")]
    public static partial Regex CinRegex();

    public static IRuleBuilderOptions<T, string> StrongPassword<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty()
            .MinimumLength(8).MaximumLength(128)
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a digit.");

    public static IRuleBuilderOptions<T, string?> MatchesUpper<T>(this IRuleBuilder<T, string?> rule, Regex regex, string name) =>
        rule.Must(v => v is null || regex.IsMatch(v.Trim().ToUpperInvariant())).WithMessage($"'{{PropertyName}}' is not a valid {name}.");
}
