using CoachOS.Application.Camps.DTOs;
using FluentValidation;

namespace CoachOS.Application.Camps.Validators;

/// <summary>Validatie van één prijsoptie bij het aanmaken of bijwerken van een kamp.</summary>
public class SaveCampPriceRequestValidator : AbstractValidator<SaveCampPriceRequest>
{
    public const int MaxPrices = 10;

    public SaveCampPriceRequestValidator()
    {
        RuleFor(p => p.Label)
            .NotEmpty().WithMessage("Naam van de prijsoptie is verplicht")
            .MaximumLength(120).WithMessage("Naam van de prijsoptie mag maximaal 120 karakters zijn");
        RuleFor(p => p.Description)
            .MaximumLength(500).WithMessage("Uitleg van de prijsoptie mag maximaal 500 karakters zijn");
        RuleFor(p => p.Discount)
            .GreaterThanOrEqualTo(0m).WithMessage("Korting van de prijsoptie mag niet negatief zijn");
    }
}
