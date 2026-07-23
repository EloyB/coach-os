using CoachOS.Application.LessonSerie.DTOs;
using FluentValidation;

namespace CoachOS.Application.LessonSerie.Validators;

public class SaveLessonSeriePricesRequestValidator : AbstractValidator<SaveLessonSeriePricesRequest>
{
    /// <summary>
    /// Bovengrens voor groepsgrootte in de matrix. Ruimer dan de 4 die Thomas noemde,
    /// zodat clubs met grotere jeugdgroepen niet vastlopen.
    /// </summary>
    private const int MaxGroupSize = 8;

    public SaveLessonSeriePricesRequestValidator()
    {
        RuleFor(x => x.Prices)
            .NotNull().WithMessage("Prijzen zijn verplicht.");

        RuleForEach(x => x.Prices).ChildRules(price =>
        {
            price.RuleFor(p => p.Category)
                .InclusiveBetween(1, 2)
                .WithMessage("Categorie moet 1 (volwassenen) of 2 (jeugd) zijn.");

            price.RuleFor(p => p.GroupSize)
                .InclusiveBetween(1, MaxGroupSize)
                .WithMessage($"Groepsgrootte moet tussen 1 en {MaxGroupSize} liggen.");

            price.RuleFor(p => p.TotalPrice)
                .GreaterThanOrEqualTo(0).WithMessage("Prijs mag niet negatief zijn.")
                .LessThanOrEqualTo(100000).WithMessage("Prijs is onrealistisch hoog.");
        });

        // Dubbele cellen zouden een niet-deterministische prijs opleveren; de unique
        // index vangt dit ook af, maar dan als HTTP 500 in plaats van een nette fout.
        RuleFor(x => x.Prices)
            .Must(prices => prices
                .GroupBy(p => (p.Category, p.GroupSize))
                .All(g => g.Count() == 1))
            .WithMessage("Er staan dubbele combinaties van categorie en groepsgrootte in de prijstabel.")
            .When(x => x.Prices is not null);
    }
}
