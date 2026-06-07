using CoachOS.Application.Common;
using CoachOS.Application.Trainers.DTOs;
using FluentValidation;

namespace CoachOS.Application.Trainers.Validators;

public class UpdateTrainerRequestValidator : AbstractValidator<UpdateTrainerRequest>
{
    public UpdateTrainerRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Voornaam is verplicht")
            .MaximumLength(100).WithMessage("Voornaam mag maximaal 100 karakters zijn")
            .Must(InputSanitizer.IsFreeOfHtml).WithMessage("Voornaam mag geen HTML of scripttekens bevatten");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Achternaam is verplicht")
            .MaximumLength(100).WithMessage("Achternaam mag maximaal 100 karakters zijn")
            .Must(InputSanitizer.IsFreeOfHtml).WithMessage("Achternaam mag geen HTML of scripttekens bevatten");

        RuleFor(x => x.WeeklyCapacityHours)
            .InclusiveBetween(1, 80).WithMessage("Weekcapaciteit moet tussen 1 en 80 uur zijn");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notitie mag maximaal 1000 karakters zijn");
    }
}
