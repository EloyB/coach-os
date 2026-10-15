using CoachOS.Application.Common;
using CoachOS.Application.Enrollments.DTOs;
using CoachOS.Domain.Common;
using FluentValidation;

namespace CoachOS.Application.Enrollments.Validators;

public class UpdateBasicEnrollmentRequestValidator : AbstractValidator<UpdateBasicEnrollmentRequest>
{
    public UpdateBasicEnrollmentRequestValidator(TimeProvider timeProvider)
    {
        DateOnly today = timeProvider.GetBrusselsToday();
        RuleFor(x => x.StudentName)
            .NotEmpty().WithMessage("Naam is verplicht")
            .MaximumLength(200);

        RuleFor(x => x.ContactEmail)
            .NotEmpty().WithMessage("Contact e-mailadres is verplicht")
            .EmailAddress().WithMessage("Ongeldig e-mailadres")
            .MaximumLength(320);

        RuleFor(x => x.StudentEmail)
            .EmailAddress().WithMessage("Ongeldig e-mailadres")
            .MaximumLength(320)
            .When(x => !string.IsNullOrWhiteSpace(x.StudentEmail));

        RuleFor(x => x.StudentPhone)
            .MaximumLength(50);

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Geboortedatum is verplicht")
            .Must(DateOfBirthRules.IsParseable).WithMessage("Geboortedatum moet het formaat yyyy-MM-dd hebben")
            .Must(v => DateOfBirthRules.IsNotInFuture(v, today)).WithMessage("Geboortedatum kan niet in de toekomst liggen")
            .Must(v => DateOfBirthRules.IsRealistic(v, today)).WithMessage("Controleer de geboortedatum");
    }
}
