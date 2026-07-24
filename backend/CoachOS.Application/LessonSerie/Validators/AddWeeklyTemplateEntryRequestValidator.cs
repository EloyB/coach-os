using CoachOS.Application.LessonSerie.DTOs;
using FluentValidation;

namespace CoachOS.Application.LessonSerie.Validators;

public class AddWeeklyTemplateEntryRequestValidator : AbstractValidator<AddWeeklyTemplateEntryRequest>
{
    public AddWeeklyTemplateEntryRequestValidator()
    {
        RuleFor(x => x.DayOfWeek)
            .InclusiveBetween(0, 6).WithMessage("Dag van de week moet tussen 0 (maandag) en 6 (zondag) liggen.");

        RuleFor(x => x.StartTime)
            .NotEmpty().WithMessage("Starttijd is verplicht.")
            .Matches(@"^\d{2}:\d{2}$").WithMessage("Starttijd moet het formaat HH:mm hebben.");

        RuleFor(x => x.EndTime)
            .NotEmpty().WithMessage("Eindtijd is verplicht.")
            .Matches(@"^\d{2}:\d{2}$").WithMessage("Eindtijd moet het formaat HH:mm hebben.");

        RuleFor(x => x)
            .Must(x => string.Compare(x.EndTime, x.StartTime, StringComparison.Ordinal) > 0)
            .WithMessage("Eindtijd moet na de starttijd liggen.")
            .WithName("EndTime")
            .When(x => IsTime(x.StartTime) && IsTime(x.EndTime));

        RuleFor(x => x.CourtName)
            .MaximumLength(100).WithMessage("Baannaam mag maximaal 100 karakters zijn.")
            .When(x => x.CourtName is not null);

        RuleFor(x => x.MaxStudents)
            .GreaterThan(0).WithMessage("Maximum aantal leerlingen moet groter dan 0 zijn.");

        RuleFor(x => x.Level)
            .InclusiveBetween(1, 5).WithMessage("Niveau moet tussen 1 en 5 liggen.")
            .When(x => x.Level.HasValue);
    }

    private static bool IsTime(string value) =>
        System.Text.RegularExpressions.Regex.IsMatch(value, @"^\d{2}:\d{2}$");
}
