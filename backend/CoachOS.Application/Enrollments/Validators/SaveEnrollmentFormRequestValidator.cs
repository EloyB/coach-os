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

            // An age-category field needs at least two buckets to group students by.
            field.RuleFor(f => f.Options)
                .Must(o => o is not null && o.Count(s => !string.IsNullOrWhiteSpace(s)) >= 2)
                .When(f => f.Type == (int)FormFieldType.AgeCategory)
                .WithMessage("Een leeftijdscategorie heeft minstens twee opties nodig");
        });

        // At most one age-category question per form, otherwise the algorithm can't tell which to group on.
        RuleFor(x => x.Fields)
            .Must(fields => fields.Count(f => f.Type == (int)FormFieldType.AgeCategory) <= 1)
            .WithMessage("Er kan maar één leeftijdscategorie-veld zijn");
    }
}
