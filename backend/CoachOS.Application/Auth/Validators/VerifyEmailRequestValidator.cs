using CoachOS.Application.Auth.DTOs;
using FluentValidation;

namespace CoachOS.Application.Auth.Validators;

public class VerifyEmailRequestValidator : AbstractValidator<VerifyEmailRequest>
{
    public VerifyEmailRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is verplicht")
            .Matches($"^[0-9]{{{EmailVerificationCodes.Length}}}$")
            .WithMessage($"De code bestaat uit {EmailVerificationCodes.Length} cijfers");
    }
}
