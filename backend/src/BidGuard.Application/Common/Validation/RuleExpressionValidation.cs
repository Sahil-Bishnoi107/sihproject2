using System.Text.Json;
using BidGuard.Application.Services.Rules;
using FluentValidation;

namespace BidGuard.Application.Common.Validation;

public static class RuleExpressionValidation
{
    /// <summary>The expression must parse under the Compliance-as-Code grammar; the first problem is reported.</summary>
    public static IRuleBuilderOptionsConditions<T, JsonElement> ValidRuleExpression<T>(this IRuleBuilder<T, JsonElement> rule) =>
        rule.Custom((expression, context) =>
        {
            if (expression.ValueKind != JsonValueKind.Object)
            {
                context.AddFailure("Expression must be a JSON object.");
                return;
            }
            foreach (var error in RuleExpression.Validate(expression.GetRawText()))
                context.AddFailure($"Invalid rule expression: {error}");
        });
}
