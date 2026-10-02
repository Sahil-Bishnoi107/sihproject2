using FluentValidation;
using Mediator;
using ValidationException = BidGuard.Application.Common.Exceptions.ValidationException;

namespace BidGuard.Application.Common.Behaviors;

/// <summary>Runs every FluentValidation validator registered for the message before its handler.</summary>
public sealed class ValidationBehavior<TMessage, TResponse>(IEnumerable<IValidator<TMessage>> validators)
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : notnull, IMessage
{
    public async ValueTask<TResponse> Handle(TMessage message, MessageHandlerDelegate<TMessage, TResponse> next, CancellationToken cancellationToken)
    {
        var validatorList = validators as IValidator<TMessage>[] ?? validators.ToArray();
        if (validatorList.Length == 0)
            return await next(message, cancellationToken);

        var context = new ValidationContext<TMessage>(message);
        var results = await Task.WhenAll(validatorList.Select(v => v.ValidateAsync(context, cancellationToken)));
        var failures = results.SelectMany(r => r.Errors).Where(f => f is not null).ToList();

        if (failures.Count > 0)
            throw new ValidationException(failures
                .GroupBy(f => f.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).Distinct().ToArray()));

        return await next(message, cancellationToken);
    }
}
