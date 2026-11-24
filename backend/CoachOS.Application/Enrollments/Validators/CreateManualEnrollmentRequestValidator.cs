using CoachOS.Application.Enrollments.DTOs;
using CoachOS.Domain.Common;
using FluentValidation;

namespace CoachOS.Application.Enrollments.Validators;

public class CreateManualEnrollmentRequestValidator : AbstractValidator<CreateManualEnrollmentRequest>
{
    public CreateManualEnrollmentRequestValidator()
    {
        RuleFor(x => x.StudentName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContactEmail).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.StudentEmail).EmailAddress().MaximumLength(320).When(x => !string.IsNullOrWhiteSpace(x.StudentEmail));
        RuleFor(x => x.StudentPhone).NotEmpty().WithMessage("Telefoonnummer is verplicht").MaximumLength(50);
        RuleFor(x => x.DateOfBirth).NotEmpty().Matches(@"^\d{4}-\d{2}-\d{2}$");

        // Maximum aantal personen per tijdslot: 1 (solo) tot 4. De ondergrens (eigen groepsgrootte) en de
        // bovengrens van de reeks controleert de service, want die hebben de reeks nodig.
        RuleFor(x => x.MaxGroupSize)
            .InclusiveBetween(1, GroupSizeRules.AbsoluteMax)
            .WithMessage($"Het maximum aantal personen moet tussen 1 en {GroupSizeRules.AbsoluteMax} liggen.")
            .When(x => x.MaxGroupSize.HasValue);

    }
}
