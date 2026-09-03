using System.Globalization;
using CoachOS.Application.LessonReschedule.DTOs;
using CoachOS.Domain.Common;
using FluentValidation;

namespace CoachOS.Application.LessonReschedule.Validators;

public class RescheduleLessonRequestValidator : AbstractValidator<RescheduleLessonRequest>
{
    public RescheduleLessonRequestValidator(TimeProvider timeProvider)
    {
        DateOnly today = timeProvider.GetBrusselsToday();

        RuleFor(x => x.NewDate)
            .NotEmpty().WithMessage("Nieuwe datum is verplicht.")
            .Must(BeValidIsoDate).WithMessage("Nieuwe datum moet in formaat yyyy-MM-dd zijn.")
            .Must(value => NotBeInThePast(value, today)).WithMessage("Nieuwe datum mag niet in het verleden liggen.");

        RuleFor(x => x.NewStartTime)
            .NotEmpty().WithMessage("Nieuwe starttijd is verplicht.")
            .Must(BeValidTime).WithMessage("Starttijd moet in formaat HH:mm zijn.");

        RuleFor(x => x.NewEndTime)
            .NotEmpty().WithMessage("Nieuwe eindtijd is verplicht.")
            .Must(BeValidTime).WithMessage("Eindtijd moet in formaat HH:mm zijn.");

        RuleFor(x => x)
            .Must(EndAfterStart).WithMessage("Eindtijd moet na de starttijd liggen.")
            .When(x => BeValidTime(x.NewStartTime) && BeValidTime(x.NewEndTime));

        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Reden mag maximaal 500 karakters zijn.");
    }

    private static bool BeValidIsoDate(string value)
        => DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out _);

    private static bool BeValidTime(string value)
        => TimeOnly.TryParseExact(value, "HH:mm", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out _);

    private static bool NotBeInThePast(string value, DateOnly today)
    {
        if (!DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateOnly parsed))
            return true; // formaat-fout wordt elders gerapporteerd
        return parsed >= today;
    }

    private static bool EndAfterStart(RescheduleLessonRequest request)
    {
        TimeOnly start = TimeOnly.ParseExact(request.NewStartTime, "HH:mm", CultureInfo.InvariantCulture);
        TimeOnly end = TimeOnly.ParseExact(request.NewEndTime, "HH:mm", CultureInfo.InvariantCulture);
        return end > start;
    }
}
