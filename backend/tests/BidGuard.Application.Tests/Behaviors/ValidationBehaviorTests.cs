using BidGuard.Application.Common.Behaviors;
using FluentAssertions;
using FluentValidation;
using Mediator;
using ValidationException = BidGuard.Application.Common.Exceptions.ValidationException;

namespace BidGuard.Application.Tests.Behaviors;

public class ValidationBehaviorTests
{
    public sealed record Ping(string Name) : ICommand<string>;

    private sealed class PingValidator : AbstractValidator<Ping>
    {
        public PingValidator() => RuleFor(x => x.Name).NotEmpty().MaximumLength(5);
    }

    private static ValueTask<string> Next(Ping message, CancellationToken ct) => ValueTask.FromResult("pong:" + message.Name);

    [Fact]
    public async Task Invalid_message_throws_ValidationException_keyed_by_property()
    {
        var behavior = new ValidationBehavior<Ping, string>([new PingValidator()]);

        var act = async () => await behavior.Handle(new Ping(""), Next, CancellationToken.None);

        var ex = (await act.Should().ThrowAsync<ValidationException>()).Which;
        ex.Errors.Should().ContainKey("Name");
    }

    [Fact]
    public async Task Valid_message_reaches_handler()
    {
        var behavior = new ValidationBehavior<Ping, string>([new PingValidator()]);

        var result = await behavior.Handle(new Ping("abc"), Next, CancellationToken.None);

        result.Should().Be("pong:abc");
    }

    [Fact]
    public async Task No_validators_passes_through()
    {
        var behavior = new ValidationBehavior<Ping, string>([]);

        var result = await behavior.Handle(new Ping(""), Next, CancellationToken.None);

        result.Should().Be("pong:");
    }
}
