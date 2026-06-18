using CoachOS.Application.Camps.DTOs;
using CoachOS.Domain.Enums;
using FluentValidation;

namespace CoachOS.Application.Camps.Validators;

public class ChooseCampPaymentRequestValidator : AbstractValidator<ChooseCampPaymentRequest>
{
    public ChooseCampPaymentRequestValidator()
    {
        RuleFor(x => x.Method)
            .Must(m => Enum.IsDefined(typeof(PaymentMethod), m))
            .WithMessage("Ongeldige betaalmethode.");
    }
}
