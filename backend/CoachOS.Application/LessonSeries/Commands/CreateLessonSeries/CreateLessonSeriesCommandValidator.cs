using FluentValidation;

namespace CoachOS.Application.LessonSeries.Commands.CreateLessonSeries;

public class CreateLessonSeriesCommandValidator : AbstractValidator<CreateLessonSeriesCommand>
{
    public CreateLessonSeriesCommandValidator()
    {
        RuleFor(x => x.OrganizationId)
            .NotEmpty().WithMessage("OrganizationId is verplicht.");

        RuleFor(x => x.TennisClubId)
            .NotEmpty().WithMessage("Tennisclub is verplicht.");

        RuleFor(x => x.TrainerId)
            .NotEmpty().WithMessage("Trainer is verplicht.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Naam is verplicht.")
            .MaximumLength(200).WithMessage("Naam mag maximaal 200 karakters zijn.");

        RuleFor(x => x.Level)
            .InclusiveBetween(1, 5).WithMessage("Niveau moet tussen 1 en 5 liggen.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Prijs mag niet negatief zijn.");

        RuleFor(x => x.DurationMinutes)
            .GreaterThan(0).WithMessage("Duur moet groter dan 0 minuten zijn.");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Startdatum is verplicht.")
            .Matches(@"^\d{4}-\d{2}-\d{2}$").WithMessage("Startdatum moet het formaat yyyy-MM-dd hebben.");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("Einddatum is verplicht.")
            .Matches(@"^\d{4}-\d{2}-\d{2}$").WithMessage("Einddatum moet het formaat yyyy-MM-dd hebben.");

        RuleFor(x => x)
            .Must(x =>
            {
                if (!DateOnly.TryParseExact(x.StartDate, "yyyy-MM-dd", out DateOnly start)) return true;
                if (!DateOnly.TryParseExact(x.EndDate, "yyyy-MM-dd", out DateOnly end)) return true;
                return end >= start;
            })
            .WithMessage("Einddatum moet op of na de startdatum liggen.")
            .WithName("EndDate");
    }
}
