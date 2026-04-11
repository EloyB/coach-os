using CoachOS.Application.LessonSerie.DTOs;
using FluentValidation;

namespace CoachOS.Application.LessonSerie.Validators;

public class UpdateLessonSerieRequestValidator : AbstractValidator<UpdateLessonSerieRequest>
{
    public UpdateLessonSerieRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Naam is verplicht.")
            .MaximumLength(200).WithMessage("Naam mag maximaal 200 karakters zijn.");

        RuleFor(x => x.Level)
            .InclusiveBetween(1, 5).WithMessage("Niveau moet tussen 1 en 5 liggen.")
            .When(x => x.Level.HasValue);

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Prijs mag niet negatief zijn.");

        RuleFor(x => x.RegistrationDeadline)
            .NotEmpty().WithMessage("Inschrijvingsdeadline is verplicht.");

        RuleFor(x => x.TennisClubId)
            .NotEmpty().WithMessage("Tennisclub is verplicht.");
    }
}
