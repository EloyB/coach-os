using FluentValidation;

namespace CoachOS.Application.LessonSeries.Commands.UpdateLessonSeries;

public class UpdateLessonSeriesCommandValidator : AbstractValidator<UpdateLessonSeriesCommand>
{
    public UpdateLessonSeriesCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is verplicht.");

        RuleFor(x => x.OrganizationId)
            .NotEmpty().WithMessage("OrganizationId is verplicht.");

        RuleFor(x => x.TrainerId)
            .NotEmpty().WithMessage("Trainer is verplicht.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Naam is verplicht.")
            .MaximumLength(200).WithMessage("Naam mag maximaal 200 karakters zijn.");

        RuleFor(x => x.Level)
            .InclusiveBetween(1, 5).WithMessage("Niveau moet tussen 1 en 5 liggen.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Prijs mag niet negatief zijn.");

        RuleFor(x => x.TennisClubId)
            .NotEmpty().WithMessage("Tennisclub is verplicht.");
    }
}
