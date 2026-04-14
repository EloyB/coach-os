using CoachOS.Application.StudentAuth.DTOs;
using FluentValidation;

namespace CoachOS.Application.StudentAuth.Validators;

public class RequestMagicLinkRequestValidator : AbstractValidator<RequestMagicLinkRequest>
{
    public RequestMagicLinkRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-mailadres is verplicht")
            .EmailAddress().WithMessage("E-mailadres is ongeldig");
    }
}
