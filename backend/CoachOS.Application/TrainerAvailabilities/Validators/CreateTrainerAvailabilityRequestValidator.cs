using System.Text.RegularExpressions;
using CoachOS.Application.TrainerAvailabilities.DTOs;
using FluentValidation;

namespace CoachOS.Application.TrainerAvailabilities.Validators;

public class CreateTrainerAvailabilityRequestValidator : AbstractValidator<CreateTrainerAvailabilityRequest>
{
    private static readonly Regex TimePattern = new(@"^([01]\d|2[0-3]):[0-5]\d$", RegexOptions.Compiled);

    public CreateTrainerAvailabilityRequestValidator()
    {
        RuleFor(x => x.TrainerId)
            .NotEmpty().WithMessage("Trainer is verplicht");

        // TennisClubId is optioneel: null = beschikbaar bij eender welke club.
        // Als er wel een waarde wordt meegegeven mag die niet Guid.Empty zijn.
        RuleFor(x => x.TennisClubId)
            .NotEqual(Guid.Empty).WithMessage("Ongeldige club")
            .When(x => x.TennisClubId.HasValue);

        RuleFor(x => x.DayOfWeek)
            .InclusiveBetween(0, 6).WithMessage("Ongeldige weekdag");

        RuleFor(x => x.StartTime)
            .Must(t => t is not null && TimePattern.IsMatch(t)).WithMessage("Ongeldige starttijd (HH:mm)");

        RuleFor(x => x.EndTime)
            .Must(t => t is not null && TimePattern.IsMatch(t)).WithMessage("Ongeldige eindtijd (HH:mm)");

        RuleFor(x => x)
            .Must(x => string.Compare(x.EndTime, x.StartTime, StringComparison.Ordinal) > 0)
            .WithMessage("Eindtijd moet na starttijd zijn")
            .When(x => x.StartTime is not null && x.EndTime is not null
                && TimePattern.IsMatch(x.StartTime) && TimePattern.IsMatch(x.EndTime));
    }
}
