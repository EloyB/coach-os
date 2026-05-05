using CoachOS.Application.Auth.DTOs;
using FluentValidation;

namespace CoachOS.Application.Auth.Validators;

public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-mailadres is verplicht")
            .EmailAddress().WithMessage("E-mailadres is ongeldig");

        RuleFor(x => x.ResetBaseUrl)
            .NotEmpty().WithMessage("ResetBaseUrl is verplicht");
    }
}
