using CoachOS.Application.Auth.DTOs;
using FluentValidation;

namespace CoachOS.Application.Auth.Validators;

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-mailadres is verplicht")
            .EmailAddress().WithMessage("E-mailadres is ongeldig");

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token is verplicht");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Wachtwoord is verplicht")
            .MinimumLength(8).WithMessage("Wachtwoord moet minimaal 8 tekens bevatten");
    }
}
