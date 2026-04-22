using CoachOS.Application.LessonSerie.DTOs;
using FluentValidation;

namespace CoachOS.Application.LessonSerie.Validators;

public class UpdateLessonRequestValidator : AbstractValidator<UpdateLessonRequest>
{
    public UpdateLessonRequestValidator()
    {
        // At least one field must be provided
        RuleFor(x => x).Must(r =>
            r.TrainerId.HasValue || r.Date is not null || r.StartTime is not null ||
            r.EndTime is not null || r.CourtName is not null || r.MaxStudents.HasValue ||
            r.Notes is not null || r.IsCancelled.HasValue
        ).WithMessage("Minstens één veld is vereist.");

        RuleFor(x => x.Date)
            .Matches(@"^\d{4}-\d{2}-\d{2}$").WithMessage("Datum moet yyyy-MM-dd zijn.")
            .Must(d => DateOnly.TryParseExact(d, "yyyy-MM-dd", out _)).WithMessage("Ongeldige datum.")
            .When(x => x.Date is not null);

        RuleFor(x => x.StartTime)
            .Matches(@"^\d{2}:\d{2}$").WithMessage("Starttijd moet HH:mm zijn.")
            .Must(t => TimeOnly.TryParseExact(t, "HH:mm", out _)).WithMessage("Ongeldige starttijd.")
            .When(x => x.StartTime is not null);

        RuleFor(x => x.EndTime)
            .Matches(@"^\d{2}:\d{2}$").WithMessage("Eindtijd moet HH:mm zijn.")
            .Must(t => TimeOnly.TryParseExact(t, "HH:mm", out _)).WithMessage("Ongeldige eindtijd.")
            .When(x => x.EndTime is not null);

        // When both start and end are provided, end must be after start
        RuleFor(x => x).Must(r =>
        {
            if (r.StartTime is null || r.EndTime is null) return true;
            TimeOnly start = TimeOnly.ParseExact(r.StartTime, "HH:mm");
            TimeOnly end = TimeOnly.ParseExact(r.EndTime, "HH:mm");
            return end > start;
        }).WithMessage("Eindtijd moet na de starttijd liggen.")
        .When(x => x.StartTime is not null && x.EndTime is not null);

        RuleFor(x => x.CourtName)
            .MaximumLength(100).WithMessage("Baannaam mag maximaal 100 tekens zijn.")
            .When(x => x.CourtName is not null);

        RuleFor(x => x.MaxStudents)
            .GreaterThan(0).WithMessage("Max. leerlingen moet groter dan 0 zijn.")
            .LessThanOrEqualTo(100).WithMessage("Max. leerlingen mag niet meer dan 100 zijn.")
            .When(x => x.MaxStudents is not null);

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notities mogen maximaal 1000 tekens zijn.")
            .When(x => x.Notes is not null);
    }
}
