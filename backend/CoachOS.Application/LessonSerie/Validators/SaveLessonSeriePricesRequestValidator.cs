using CoachOS.Application.LessonSerie.DTOs;
using FluentValidation;

namespace CoachOS.Application.LessonSerie.Validators;

public class SaveLessonSeriePricesRequestValidator : AbstractValidator<SaveLessonSeriePricesRequest>
{
    public SaveLessonSeriePricesRequestValidator()
    {
        RuleFor(x => x.Prices)
            .NotNull().WithMessage("Prijzen zijn verplicht.");

        RuleForEach(x => x.Prices).ChildRules(price =>
        {
            price.RuleFor(p => p.Label)
                .NotEmpty().WithMessage("Prijsoptie heeft een naam nodig.")
                .MaximumLength(120).WithMessage("Naam van de prijsoptie is te lang.");

            price.RuleFor(p => p.Description)
                .MaximumLength(500).WithMessage("Beschrijving van de prijsoptie is te lang.");

            price.RuleFor(p => p.TotalPrice)
                .GreaterThanOrEqualTo(0).WithMessage("Prijs mag niet negatief zijn.")
                .LessThanOrEqualTo(100000).WithMessage("Prijs is onrealistisch hoog.");

            price.RuleFor(p => p.ReusableKey)
                .MaximumLength(120).WithMessage("Herbruikbare sleutel is te lang.");
        });

        RuleFor(x => x.Prices)
            .Must(HaveUniqueLabels)
            .WithMessage("Er staan twee prijsopties met dezelfde naam in de lijst.")
            .When(x => x.Prices is not null);
    }

    private static bool HaveUniqueLabels(IEnumerable<LessonSeriePriceRequest> prices)
    {
        return prices
            .GroupBy(p => p.Label.Trim().ToLowerInvariant())
            .All(g => g.Count() == 1);
    }
}
