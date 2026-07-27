using CoachOS.Application.LessonSerie.DTOs;
using CoachOS.Domain.Enums;
using FluentValidation;

namespace CoachOS.Application.LessonSerie.Validators;

public class SaveLessonSeriePricesRequestValidator : AbstractValidator<SaveLessonSeriePricesRequest>
{
    private const int MaxGroupSize = 8;

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

            price.RuleFor(p => p.Mode)
                .Must(v => Enum.IsDefined(typeof(PricingMode), v))
                .WithMessage("Onbekende pricing mode.");

            price.RuleFor(p => p.TotalPrice)
                .GreaterThanOrEqualTo(0).WithMessage("Prijs mag niet negatief zijn.")
                .LessThanOrEqualTo(100000).WithMessage("Prijs is onrealistisch hoog.");

            price.RuleFor(p => p.GroupSize)
                .NotNull().WithMessage("Groepsgrootte is verplicht voor prijs per groepsgrootte.")
                .InclusiveBetween(1, MaxGroupSize)
                .When(p => p.Mode == (int)PricingMode.GroupSize);

            price.RuleFor(p => p.GroupSize)
                .Null().WithMessage("Groepsgrootte hoort alleen bij prijs per groepsgrootte.")
                .When(p => p.Mode != (int)PricingMode.GroupSize);

            price.RuleFor(p => p.Category)
                .NotNull().WithMessage("Tariefcategorie is verplicht voor categorieprijzen.")
                .Must(v => v is null || Enum.IsDefined(typeof(ParticipantCategory), v.Value))
                .WithMessage("Onbekende tariefcategorie.")
                .When(p => p.Mode == (int)PricingMode.TariffCategory);

            price.RuleFor(p => p.Category)
                .Null().WithMessage("Tariefcategorie hoort alleen bij categorieprijzen.")
                .When(p => p.Mode != (int)PricingMode.TariffCategory);

            price.RuleFor(p => p.ReusableKey)
                .MaximumLength(120).WithMessage("Herbruikbare sleutel is te lang.");
        });

        RuleFor(x => x.Prices)
            .Must(HaveUniqueAutomaticRules)
            .WithMessage("Er staan dubbele prijsregels in de lijst.")
            .When(x => x.Prices is not null);
    }

    private static bool HaveUniqueAutomaticRules(IEnumerable<LessonSeriePriceRequest> prices)
    {
        return prices
            .GroupBy(p => (p.Mode, p.GroupSize, p.Category, ManualKey: p.Mode == (int)PricingMode.ManualOption ? p.Label.Trim().ToLowerInvariant() : null))
            .All(g => g.Count() == 1);
    }
}
