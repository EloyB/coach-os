using CoachOS.Application.Billing.DTOs;
using CoachOS.Domain.Enums;
using FluentValidation;

namespace CoachOS.Application.Billing.Validators;

public class CheckoutRequestValidator : AbstractValidator<CheckoutRequest>
{
    public CheckoutRequestValidator()
    {
        RuleFor(x => x.Plan).Must(v => Enum.IsDefined(typeof(SubscriptionPlan), v)).WithMessage("Ongeldig plan");
        RuleFor(x => x.Interval).Must(v => Enum.IsDefined(typeof(BillingInterval), v)).WithMessage("Ongeldige facturatieperiode");
        RuleFor(x => x.Method).Must(v => Enum.IsDefined(typeof(BillingPaymentMethod), v)).WithMessage("Ongeldige betaalwijze");
    }
}
