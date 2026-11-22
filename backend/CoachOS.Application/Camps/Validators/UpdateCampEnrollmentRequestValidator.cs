using System.Text.RegularExpressions;
using CoachOS.Application.Camps.DTOs;
using CoachOS.Application.Common;
using CoachOS.Domain.Common;
using FluentValidation;

namespace CoachOS.Application.Camps.Validators;

public class UpdateCampEnrollmentRequestValidator : AbstractValidator<UpdateCampEnrollmentRequest>
{
    private static readonly Regex EmailPattern = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public UpdateCampEnrollmentRequestValidator(TimeProvider timeProvider)
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
            .Must(DateOfBirthRules.IsParseable).WithMessage("Geboortedatum moet het formaat yyyy-MM-dd hebben")
            .Must(v => DateOfBirthRules.IsNotInFuture(v, today)).WithMessage("Geboortedatum kan niet in de toekomst liggen")
            .Must(v => DateOfBirthRules.IsRealistic(v, today)).WithMessage("Controleer de geboortedatum")
            .When(x => !string.IsNullOrWhiteSpace(x.DateOfBirth));
        RuleFor(x => x.ParticipantPhone)
            .MaximumLength(50).WithMessage("Telefoonnummer mag maximaal 50 karakters zijn");
    }
}
