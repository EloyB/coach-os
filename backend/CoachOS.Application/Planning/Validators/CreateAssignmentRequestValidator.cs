using CoachOS.Application.Planning.DTOs;
using FluentValidation;

namespace CoachOS.Application.Planning.Validators;

public class CreateAssignmentRequestValidator : AbstractValidator<CreateAssignmentRequest>
{
    public CreateAssignmentRequestValidator()
    {
        RuleFor(x => x.WeeklyTemplateEntryId)
            .NotEmpty().WithMessage("Tijdslot is verplicht");

        RuleFor(x => x)
            .Must(r => r.EnrollmentId.HasValue ^ r.GroupId.HasValue)
            .WithMessage("Geef óf een enrollmentId óf een groupId (exact één).");
    }
}
