using CoachOS.Application.Enrollments.DTOs;
using FluentValidation;

namespace CoachOS.Application.Enrollments.Validators;

public class SubmitEnrollmentRequestValidator : AbstractValidator<SubmitEnrollmentRequest>
{
    public SubmitEnrollmentRequestValidator()
    {
        RuleFor(x => x.StudentName)
            .NotEmpty().WithMessage("Naam is verplicht")
            .MaximumLength(200).WithMessage("Naam mag maximaal 200 karakters zijn");

        RuleFor(x => x.StudentEmail)
            .NotEmpty().WithMessage("E-mailadres is verplicht")
            .EmailAddress().WithMessage("Ongeldig e-mailadres");

        RuleForEach(x => x.Responses).ChildRules(r =>
        {
            r.RuleFor(v => v.FormFieldId)
                .NotEmpty().WithMessage("FormFieldId is verplicht");

            r.RuleFor(v => v.Value)
                .MaximumLength(1000).WithMessage("Antwoord mag maximaal 1000 karakters zijn");
        });
    }
}
