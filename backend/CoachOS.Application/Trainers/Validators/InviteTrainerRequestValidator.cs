using CoachOS.Application.Common;
using CoachOS.Application.Trainers.DTOs;
using FluentValidation;

namespace CoachOS.Application.Trainers.Validators;

public class InviteTrainerRequestValidator : AbstractValidator<InviteTrainerRequest>
{
    public InviteTrainerRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Voornaam is verplicht")
            .MaximumLength(100).WithMessage("Voornaam mag maximaal 100 karakters zijn")
            .Must(InputSanitizer.IsFreeOfHtml).WithMessage("Voornaam mag geen HTML of scripttekens bevatten");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Achternaam is verplicht")
            .MaximumLength(100).WithMessage("Achternaam mag maximaal 100 karakters zijn")
            .Must(InputSanitizer.IsFreeOfHtml).WithMessage("Achternaam mag geen HTML of scripttekens bevatten");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-mail is verplicht")
            .MaximumLength(254).WithMessage("E-mailadres is te lang")
            .EmailAddress().WithMessage("Ongeldig e-mailadres");
    }
}
