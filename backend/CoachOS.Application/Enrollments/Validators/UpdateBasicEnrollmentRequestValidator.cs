using CoachOS.Application.Common;
using CoachOS.Application.Enrollments.DTOs;
using FluentValidation;

namespace CoachOS.Application.Enrollments.Validators;

public class UpdateBasicEnrollmentRequestValidator : AbstractValidator<UpdateBasicEnrollmentRequest>
{
    public UpdateBasicEnrollmentRequestValidator()
    {
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
            .Must(DateOfBirthRules.IsNotInFuture).WithMessage("Geboortedatum kan niet in de toekomst liggen")
            .Must(DateOfBirthRules.IsRealistic).WithMessage("Controleer de geboortedatum");
    }
}
