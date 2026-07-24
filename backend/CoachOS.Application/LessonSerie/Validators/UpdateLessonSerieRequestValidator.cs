using CoachOS.Application.Common;
using CoachOS.Application.LessonSerie.DTOs;
using FluentValidation;

namespace CoachOS.Application.LessonSerie.Validators;

public class UpdateLessonSerieRequestValidator : AbstractValidator<UpdateLessonSerieRequest>
{
    public UpdateLessonSerieRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Naam is verplicht.")
            .MaximumLength(100).WithMessage("Naam mag maximaal 100 karakters zijn.")
            .Must(InputSanitizer.IsFreeOfHtml).WithMessage("Naam mag geen HTML of scripttekens bevatten.");

        RuleFor(x => x.Level)
            .InclusiveBetween(1, 5).WithMessage("Niveau moet tussen 1 en 5 liggen.")
            .When(x => x.Level.HasValue);

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Prijs mag niet negatief zijn.")
            .LessThanOrEqualTo(100000m).WithMessage("Prijs is onrealistisch hoog.");

        RuleFor(x => x.MaxRegistrations)
            .GreaterThan(0).WithMessage("Maximum aantal inschrijvingen moet groter dan 0 zijn.")
            .LessThanOrEqualTo(500).WithMessage("Maximum aantal inschrijvingen mag niet meer dan 500 zijn.")
            .When(x => x.MaxRegistrations.HasValue);

        RuleFor(x => x.MinAge)
            .InclusiveBetween(0, 120).WithMessage("Minimumleeftijd moet tussen 0 en 120 liggen.");

        RuleFor(x => x.MaxAge)
            .InclusiveBetween(0, 120).WithMessage("Maximumleeftijd moet tussen 0 en 120 liggen.");

        RuleFor(x => x)
            .Must(x => x.MinAge <= x.MaxAge)
            .WithMessage("Minimumleeftijd mag niet groter zijn dan de maximumleeftijd.")
            .WithName("MinAge");

        RuleFor(x => x.RegistrationDeadline)
            .NotEmpty().WithMessage("Inschrijvingsdeadline is verplicht.");

        RuleFor(x => x.TennisClubId)
            .NotEmpty().WithMessage("Tennisclub is verplicht.");

        RuleFor(x => x.AllowSoloEnrollment)
            .Must((req, _) => req.AllowSoloEnrollment || req.AllowGroupEnrollment)
            .WithMessage("Kies minstens één inschrijfwijze (solo of groep).");

        RuleFor(x => x.AcceptOnlinePayment)
            .Must((req, _) => req.AcceptOnlinePayment || req.AcceptManualPayment)
            .WithMessage("Kies minstens één betaalmethode (online of overschrijving).");
    }
}
