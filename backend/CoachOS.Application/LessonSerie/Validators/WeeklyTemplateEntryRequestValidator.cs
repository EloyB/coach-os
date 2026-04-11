using CoachOS.Application.LessonSerie.DTOs;
using FluentValidation;

namespace CoachOS.Application.LessonSerie.Validators;

public class WeeklyTemplateEntryRequestValidator : AbstractValidator<WeeklyTemplateEntryRequest>
{
    public WeeklyTemplateEntryRequestValidator()
    {
        RuleFor(x => x.DayOfWeek)
            .InclusiveBetween(0, 6).WithMessage("Dag van de week moet tussen 0 (maandag) en 6 (zondag) liggen.");

        RuleFor(x => x.StartTime)
            .NotEmpty().WithMessage("Starttijd is verplicht.")
            .Matches(@"^\d{2}:\d{2}$").WithMessage("Starttijd moet het formaat HH:mm hebben.");

        RuleFor(x => x.EndTime)
            .NotEmpty().WithMessage("Eindtijd is verplicht.")
            .Matches(@"^\d{2}:\d{2}$").WithMessage("Eindtijd moet het formaat HH:mm hebben.");

        RuleFor(x => x.CourtName)
            .MaximumLength(100).WithMessage("Baannaam mag maximaal 100 karakters zijn.")
            .When(x => x.CourtName is not null);

        RuleFor(x => x.MaxStudents)
            .GreaterThan(0).WithMessage("Maximum aantal leerlingen moet groter dan 0 zijn.");
    }
}
