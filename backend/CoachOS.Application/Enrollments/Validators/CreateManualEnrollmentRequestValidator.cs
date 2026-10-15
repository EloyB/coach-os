using CoachOS.Application.Enrollments.DTOs;
using FluentValidation;

namespace CoachOS.Application.Enrollments.Validators;

public class CreateManualEnrollmentRequestValidator : AbstractValidator<CreateManualEnrollmentRequest>
{
    public CreateManualEnrollmentRequestValidator()
    {
        RuleFor(x => x.StudentName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContactEmail).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.StudentEmail).EmailAddress().MaximumLength(320).When(x => !string.IsNullOrWhiteSpace(x.StudentEmail));
        RuleFor(x => x.StudentPhone).MaximumLength(50);
        RuleFor(x => x.DateOfBirth).NotEmpty().Matches(@"^\d{4}-\d{2}-\d{2}$");
    }
}
