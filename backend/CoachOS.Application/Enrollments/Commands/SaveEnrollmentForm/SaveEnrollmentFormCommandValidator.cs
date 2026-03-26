using CoachOS.Domain.Enums;
using FluentValidation;

namespace CoachOS.Application.Enrollments.Commands.SaveEnrollmentForm;

public class SaveEnrollmentFormCommandValidator : AbstractValidator<SaveEnrollmentFormCommand>
{
    public SaveEnrollmentFormCommandValidator()
    {
        RuleFor(x => x.LessonSeriesId)
            .NotEmpty().WithMessage("LessonSeriesId is verplicht");

        RuleFor(x => x.OrganizationId)
            .NotEmpty().WithMessage("OrganizationId is verplicht");

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
