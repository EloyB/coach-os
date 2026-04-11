using CoachOS.Application.LessonSerie.DTOs;
using FluentValidation;

namespace CoachOS.Application.LessonSerie.Validators;

public class CreateLessonSerieRequestValidator : AbstractValidator<CreateLessonSerieRequest>
{
    public CreateLessonSerieRequestValidator()
    {
        RuleFor(x => x.TennisClubId)
            .NotEmpty().WithMessage("Tennisclub is verplicht.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Naam is verplicht.")
            .MaximumLength(200).WithMessage("Naam mag maximaal 200 karakters zijn.");

        RuleFor(x => x.Level)
            .InclusiveBetween(1, 5).WithMessage("Niveau moet tussen 1 en 5 liggen.")
            .When(x => x.Level.HasValue);

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Prijs mag niet negatief zijn.");

        RuleFor(x => x.MaxRegistrations)
            .GreaterThan(0).WithMessage("Maximum aantal inschrijvingen moet groter dan 0 zijn.")
            .When(x => x.MaxRegistrations.HasValue);

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Startdatum is verplicht.")
            .Matches(@"^\d{4}-\d{2}-\d{2}$").WithMessage("Startdatum moet het formaat yyyy-MM-dd hebben.");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("Einddatum is verplicht.")
            .Matches(@"^\d{4}-\d{2}-\d{2}$").WithMessage("Einddatum moet het formaat yyyy-MM-dd hebben.");

        RuleFor(x => x.RegistrationDeadline)
            .NotEmpty().WithMessage("Inschrijvingsdeadline is verplicht.");

        RuleFor(x => x)
            .Must(x =>
            {
                if (!DateOnly.TryParseExact(x.StartDate, "yyyy-MM-dd", out var start)) return true;
                if (!DateOnly.TryParseExact(x.EndDate, "yyyy-MM-dd", out var end)) return true;
                return end >= start;
            })
            .WithMessage("Einddatum moet op of na de startdatum liggen.")
            .WithName("EndDate");

        RuleFor(x => x.Lessons)
            .NotEmpty().WithMessage("Minstens één les is verplicht.");

        RuleForEach(x => x.WeeklyTemplate)
            .SetValidator(new WeeklyTemplateEntryRequestValidator());

        RuleForEach(x => x.Lessons)
            .SetValidator(new CreateLessonRequestValidator());
    }
}
