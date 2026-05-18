using FluentValidation;
using SummaryService.Application.DTOs;
using SummaryService.Domain.Constants;

namespace SummaryService.Application.Validators;

public sealed class SummaryRequestValidator : AbstractValidator<SummaryRequest>
{
    public SummaryRequestValidator()
    {
        RuleFor(x => x.Document).NotNull();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ContentType)
            .NotEmpty()
            .Must(ct => AppConstants.AllowedMimeTypes.Contains(ct))
            .WithMessage("Only PDF and TXT files are supported");
    }
}
