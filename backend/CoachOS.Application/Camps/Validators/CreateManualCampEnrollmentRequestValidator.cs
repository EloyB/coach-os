using System.Text.RegularExpressions;
using CoachOS.Application.Camps.DTOs;
using CoachOS.Application.Common;
using CoachOS.Domain.Common;
using FluentValidation;

namespace CoachOS.Application.Camps.Validators;

public class CreateManualCampEnrollmentRequestValidator : AbstractValidator<CreateManualCampEnrollmentRequest>
{
    private static readonly Regex EmailPattern = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public CreateManualCampEnrollmentRequestValidator(TimeProvider timeProvider)
    {
        DateOnly today = timeProvider.GetBrusselsToday();
        RuleFor(x => x.ParticipantName)
            .NotEmpty().WithMessage("Naam is verplicht")
            .MaximumLength(200).WithMessage("Naam mag maximaal 200 karakters zijn");
        RuleFor(x => x.ParticipantEmail)
            .NotEmpty().WithMessage("E-mailadres is verplicht")
            .Must(e => EmailPattern.IsMatch(e)).WithMessage("Ongeldig e-mailadres")
            .MaximumLength(320).WithMessage("E-mailadres mag maximaal 320 karakters zijn");
        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Geboortedatum is verplicht")
            .Must(DateOfBirthRules.IsParseable).WithMessage("Geboortedatum moet het formaat yyyy-MM-dd hebben")
            .Must(v => DateOfBirthRules.IsNotInFuture(v, today)).WithMessage("Geboortedatum kan niet in de toekomst liggen")
            .Must(v => DateOfBirthRules.IsRealistic(v, today)).WithMessage("Controleer de geboortedatum");
        RuleFor(x => x.ParticipantPhone)
            .NotEmpty().WithMessage("Telefoonnummer is verplicht")
            .MaximumLength(50).WithMessage("Telefoonnummer mag maximaal 50 karakters zijn");
    }
}
