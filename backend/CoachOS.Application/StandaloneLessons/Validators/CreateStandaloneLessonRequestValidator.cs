using CoachOS.Application.StandaloneLessons.DTOs;
using FluentValidation;

namespace CoachOS.Application.StandaloneLessons.Validators;

public class CreateStandaloneLessonRequestValidator : AbstractValidator<CreateStandaloneLessonRequest>
{
    private static readonly int[] AllowedDurations = [30, 45, 60, 90, 120];

    public CreateStandaloneLessonRequestValidator()
    {
        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("Datum is verplicht.")
            .Matches(@"^\d{4}-\d{2}-\d{2}$").WithMessage("Datum moet het formaat yyyy-MM-dd hebben.");

        RuleFor(x => x.StartTime)
            .NotEmpty().WithMessage("Starttijd is verplicht.")
            .Matches(@"^\d{2}:\d{2}$").WithMessage("Starttijd moet het formaat HH:mm hebben.");

        RuleFor(x => x.DurationMinutes)
            .Must(d => AllowedDurations.Contains(d))
            .WithMessage("Duur moet 30, 45, 60, 90 of 120 minuten zijn.");

        RuleFor(x => x.CourtName)
            .NotEmpty().WithMessage("Baan is verplicht.")
            .MaximumLength(100).WithMessage("Baannaam mag maximaal 100 karakters zijn.");

        RuleFor(x => x.Level)
            .InclusiveBetween(1, 3).WithMessage("Niveau moet tussen 1 en 3 liggen.")
            .When(x => x.Level.HasValue);

        RuleFor(x => x.TrainerId)
            .NotEmpty().WithMessage("Trainer is verplicht.");

        RuleFor(x => x.MaxParticipants)
            .GreaterThanOrEqualTo(0).WithMessage("Maximum aantal deelnemers mag niet negatief zijn.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notities mogen maximaal 1000 karakters zijn.")
            .When(x => x.Notes is not null);

        RuleFor(x => x.ParticipantEmails)
            .NotNull().WithMessage("Deelnemers zijn verplicht.")
            .Must(emails => emails.Count > 0).WithMessage("Minstens één deelnemer is verplicht.");

        RuleForEach(x => x.ParticipantEmails)
            .NotEmpty().WithMessage("E-mailadres mag niet leeg zijn.")
            .EmailAddress().WithMessage("Ongeldig e-mailadres.")
            .MaximumLength(254).WithMessage("E-mailadres is te lang.");
    }
}
