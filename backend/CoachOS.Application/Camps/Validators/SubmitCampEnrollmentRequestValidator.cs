using System.Text.RegularExpressions;
using CoachOS.Application.Camps.DTOs;
using CoachOS.Application.Common;
using CoachOS.Domain.Common;
using FluentValidation;

namespace CoachOS.Application.Camps.Validators;

public class SubmitCampEnrollmentRequestValidator : AbstractValidator<SubmitCampEnrollmentRequest>
{
    private static readonly Regex EmailPattern = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public SubmitCampEnrollmentRequestValidator(TimeProvider timeProvider)
    {
        DateOnly today = timeProvider.GetBrusselsToday();
        RuleFor(x => x.ParticipantName).NotEmpty().WithMessage("Naam is verplicht");
        RuleFor(x => x.ParticipantEmail)
            .NotEmpty().WithMessage("E-mailadres is verplicht")
            .Must(e => EmailPattern.IsMatch(e)).WithMessage("Ongeldig e-mailadres");
        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Geboortedatum is verplicht")
            .Must(DateOfBirthRules.IsParseable).WithMessage("Geboortedatum moet het formaat yyyy-MM-dd hebben")
            .Must(v => DateOfBirthRules.IsNotInFuture(v, today)).WithMessage("Geboortedatum kan niet in de toekomst liggen")
            .Must(v => DateOfBirthRules.IsRealistic(v, today)).WithMessage("Controleer de geboortedatum");
        RuleFor(x => x.ParticipantPhone).NotEmpty().WithMessage("Telefoonnummer is verplicht");

        RuleForEach(x => x.GroupMembers).ChildRules(member =>
        {
            member.RuleFor(m => m.ParticipantName).NotEmpty().WithMessage("Naam groepslid is verplicht");
            member.RuleFor(m => m.DateOfBirth)
                .NotEmpty().WithMessage("Geboortedatum groepslid is verplicht")
                .Must(DateOfBirthRules.IsParseable).WithMessage("Geboortedatum groepslid moet het formaat yyyy-MM-dd hebben")
                .Must(v => DateOfBirthRules.IsNotInFuture(v, today)).WithMessage("Geboortedatum groepslid kan niet in de toekomst liggen")
                .Must(v => DateOfBirthRules.IsRealistic(v, today)).WithMessage("Controleer de geboortedatum van het groepslid");
            member.RuleFor(m => m.ParticipantEmail)
                .NotEmpty().WithMessage("E-mailadres groepslid is verplicht")
                .Must(e => EmailPattern.IsMatch(e)).WithMessage("Ongeldig e-mailadres groepslid");
        }).When(x => x.EnrollmentType == "group" && x.GroupMembers is not null);
    }
}
