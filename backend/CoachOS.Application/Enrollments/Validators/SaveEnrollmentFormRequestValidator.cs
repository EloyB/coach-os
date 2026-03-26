using CoachOS.Application.Enrollments.DTOs;
using CoachOS.Domain.Enums;
using FluentValidation;

namespace CoachOS.Application.Enrollments.Validators;

public class SaveEnrollmentFormRequestValidator : AbstractValidator<SaveEnrollmentFormRequest>
{
    public SaveEnrollmentFormRequestValidator()
    {
        RuleForEach(x => x.Fields).ChildRules(field =>
        {
            field.RuleFor(f => f.Label)
                .NotEmpty().WithMessage("Veldlabel is verplicht")
                .MaximumLength(200).WithMessage("Label mag maximaal 200 karakters zijn");

            field.RuleFor(f => f.Type)
                .Must(t => Enum.IsDefined(typeof(FormFieldType), t))
                .WithMessage("Ongeldig veldtype");
        });
    }
}
