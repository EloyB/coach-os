using CoachOS.Application.LessonSeries.DTOs;
using FluentValidation;

namespace CoachOS.Application.LessonSeries.Validators;

public class CreateLessonRequestValidator : AbstractValidator<CreateLessonRequest>
{
    public CreateLessonRequestValidator()
    {
        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("Datum is verplicht.")
            .Matches(@"^\d{4}-\d{2}-\d{2}$").WithMessage("Datum moet het formaat yyyy-MM-dd hebben.");

        RuleFor(x => x.StartTime)
            .NotEmpty().WithMessage("Starttijd is verplicht.")
            .Matches(@"^\d{2}:\d{2}$").WithMessage("Starttijd moet het formaat HH:mm hebben.");

        RuleFor(x => x.CourtName)
            .NotEmpty().WithMessage("Baannaam is verplicht.")
            .MaximumLength(100).WithMessage("Baannaam mag maximaal 100 karakters zijn.");
    }
}
