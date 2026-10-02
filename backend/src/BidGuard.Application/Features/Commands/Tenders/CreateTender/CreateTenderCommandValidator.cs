using FluentValidation;

namespace BidGuard.Application.Features.Commands.Tenders.CreateTender;

public sealed class CreateTenderCommandValidator : AbstractValidator<CreateTenderCommand>
{
    public CreateTenderCommandValidator(TimeProvider time)
    {
        RuleFor(x => x.GemBidNumber).NotEmpty().MaximumLength(64)
            .Matches("^GEM/[0-9]{4}/[A-Z]/[0-9]+$").WithMessage("GeM bid number must look like GEM/2026/B/1234567.");
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(200);
        RuleFor(x => x.EstimatedValue).GreaterThan(0);
        RuleFor(x => x.EmdAmount).GreaterThanOrEqualTo(0).LessThanOrEqualTo(x => x.EstimatedValue);
        RuleFor(x => x.BidEndDate).Must(d => d.ToUniversalTime() > time.GetUtcNow().UtcDateTime)
            .WithMessage("Bid end date must be in the future.");
    }
}
