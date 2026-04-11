using CoachOS.Application.LessonSerie.DTOs;
using FluentValidation;

namespace CoachOS.Application.LessonSerie.Validators;

public class CreateLessonRequestValidator : AbstractValidator<CreateLessonRequest>
{
    public CreateLessonRequestValidator()
    {
        RuleFor(x => x.TrainerId)
            .NotEmpty().WithMessage("Trainer is verplicht.");

        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("Datum is verplicht.")
            .Matches(@"^\d{4}-\d{2}-\d{2}$").WithMessage("Datum moet het formaat yyyy-MM-dd hebben.");

        RuleFor(x => x.StartTime)
            .NotEmpty().WithMessage("Starttijd is verplicht.")
            .Matches(@"^\d{2}:\d{2}$").WithMessage("Starttijd moet het formaat HH:mm hebben.");

        RuleFor(x => x.EndTime)
            .NotEmpty().WithMessage("Eindtijd is verplicht.")
            .Matches(@"^\d{2}:\d{2}$").WithMessage("Eindtijd moet het formaat HH:mm hebben.");

        RuleFor(x => x.CourtName)
            .NotEmpty().WithMessage("Baannaam is verplicht.")
            .MaximumLength(100).WithMessage("Baannaam mag maximaal 100 karakters zijn.");

        RuleFor(x => x.Level)
            .InclusiveBetween(1, 5).WithMessage("Niveau moet tussen 1 en 5 liggen.")
            .When(x => x.Level.HasValue);
    }
}
