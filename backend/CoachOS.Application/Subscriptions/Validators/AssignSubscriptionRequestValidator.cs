using System.Globalization;
using CoachOS.Application.Subscriptions.DTOs;
using CoachOS.Domain.Enums;
using FluentValidation;

namespace CoachOS.Application.Subscriptions.Validators;

public class AssignSubscriptionRequestValidator : AbstractValidator<AssignSubscriptionRequest>
{
    public AssignSubscriptionRequestValidator()
    {
        RuleFor(x => x.Plan)
            .Must(p => Enum.IsDefined(typeof(SubscriptionPlan), p))
            .WithMessage("Ongeldig plan.");

        RuleFor(x => x.Interval)
            .Must(i => Enum.IsDefined(typeof(BillingInterval), i))
            .WithMessage("Ongeldige facturatieperiode.");

        RuleFor(x => x.StartDate)
            .Must(BeValidDate)
            .When(x => !string.IsNullOrWhiteSpace(x.StartDate))
            .WithMessage("Startdatum moet in het formaat jjjj-MM-dd staan.");
    }

    private static bool BeValidDate(string? value)
        => DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
}
