using CoachOS.Application.Trainers.DTOs;
using FluentValidation;

namespace CoachOS.Application.Trainers.Validators;

public class AcceptInviteRequestValidator : AbstractValidator<AcceptInviteRequest>
{
    public AcceptInviteRequestValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token is verplicht");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Wachtwoord is verplicht")
            .MinimumLength(8).WithMessage("Wachtwoord moet minimaal 8 karakters zijn");
    }
}
