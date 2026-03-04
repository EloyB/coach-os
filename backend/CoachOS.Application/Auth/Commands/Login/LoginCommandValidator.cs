using FluentValidation;

namespace CoachOS.Application.Auth.Commands.Login;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-mailadres is verplicht")
            .EmailAddress().WithMessage("E-mailadres is ongeldig");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Wachtwoord is verplicht");
    }
}
