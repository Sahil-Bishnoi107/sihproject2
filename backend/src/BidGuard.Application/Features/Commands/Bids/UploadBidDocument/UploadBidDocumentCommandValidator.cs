using BidGuard.Application.Services.Documents;
using FluentValidation;

namespace BidGuard.Application.Features.Commands.Bids.UploadBidDocument;

public sealed class UploadBidDocumentCommandValidator : AbstractValidator<UploadBidDocumentCommand>
{
    public UploadBidDocumentCommandValidator()
    {
        RuleFor(x => x.BidId).NotEmpty();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255)
            .Must(UploadLimits.IsAllowedBidDocument).WithMessage("Bid documents must be PDF, PNG or JPEG.");
        RuleFor(x => x.Length).GreaterThan(0).LessThanOrEqualTo(UploadLimits.MaxBytes)
            .WithMessage($"File must be between 1 byte and {UploadLimits.MaxBytes / (1024 * 1024)} MB.");
        RuleFor(x => x.DocType).MaximumLength(64).Matches("^[a-z0-9_]+$").When(x => x.DocType is not null);
    }
}
