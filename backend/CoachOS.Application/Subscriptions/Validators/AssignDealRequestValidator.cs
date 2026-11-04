using System.Globalization;
using CoachOS.Application.Subscriptions.DTOs;
using CoachOS.Domain.Enums;
using FluentValidation;

namespace CoachOS.Application.Subscriptions.Validators;

public class AssignDealRequestValidator : AbstractValidator<AssignDealRequest>
{
    public AssignDealRequestValidator()
    {
        RuleFor(x => x.Plan).Must(v => Enum.IsDefined(typeof(SubscriptionPlan), v)).WithMessage("Ongeldig plan");
        RuleFor(x => x.Price).InclusiveBetween(0m, 100_000m).WithMessage("Prijs moet tussen 0 en 100000 liggen");
        RuleFor(x => x.EndDate)
            .Must(v => DateOnly.TryParseExact(v, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            .WithMessage("Einddatum moet in het formaat jjjj-mm-dd");
    }
}
