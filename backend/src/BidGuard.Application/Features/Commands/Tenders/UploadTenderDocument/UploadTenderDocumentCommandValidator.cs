using BidGuard.Application.Services.Documents;
using FluentValidation;

namespace BidGuard.Application.Features.Commands.Tenders.UploadTenderDocument;

public sealed class UploadTenderDocumentCommandValidator : AbstractValidator<UploadTenderDocumentCommand>
{
    public UploadTenderDocumentCommandValidator()
    {
        RuleFor(x => x.TenderId).NotEmpty();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255)
            .Must(UploadLimits.IsPdf).WithMessage("Tender document must be a PDF.");
        RuleFor(x => x.Length).GreaterThan(0).LessThanOrEqualTo(UploadLimits.MaxBytes)
            .WithMessage($"File must be between 1 byte and {UploadLimits.MaxBytes / (1024 * 1024)} MB.");
    }
}
