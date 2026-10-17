using CoachOS.Application.SuperAdmin.DTOs;
using FluentValidation;

namespace CoachOS.Application.SuperAdmin.Validators;

public class ExtendTrialRequestValidator : AbstractValidator<ExtendTrialRequest>
{
    public const int MaxDays = 90;

    public ExtendTrialRequestValidator()
    {
        RuleFor(x => x.Days)
            .InclusiveBetween(1, MaxDays)
            .WithMessage($"Aantal dagen moet tussen 1 en {MaxDays} liggen");
    }
}
