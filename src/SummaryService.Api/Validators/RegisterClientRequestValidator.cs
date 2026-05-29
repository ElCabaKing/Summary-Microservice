using FluentValidation;
using SummaryService.Api.Endpoints;

namespace SummaryService.Api.Validators;

public sealed class RegisterClientRequestValidator
    : AbstractValidator<RegisterClientRequest>
{
    public RegisterClientRequestValidator()
    {
        RuleFor(x => x.CompanyName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Domain)
            .NotEmpty()
            .Must(BeAbsoluteUrl)
            .WithMessage("Domain must be a valid absolute URL with http:// or https:// scheme.");
    }

    private static bool BeAbsoluteUrl(string domain)
    {
        return Uri.TryCreate(domain, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
