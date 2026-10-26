using System.Globalization;
using CoachOS.Application.Camps.DTOs;
using FluentValidation;

namespace CoachOS.Application.Camps.Validators;

/// <summary>Validatie van één pakket bij het aanmaken of bijwerken van een kamp.</summary>
public class SaveCampPackageRequestValidator : AbstractValidator<SaveCampPackageRequest>
{
    public const int MaxPackages = 10;
    private const string DateFormat = "yyyy-MM-dd";

    public SaveCampPackageRequestValidator()
    {
        RuleFor(p => p.Name)
            .NotEmpty().WithMessage("Naam van het pakket is verplicht")
            .MaximumLength(120).WithMessage("Naam van het pakket mag maximaal 120 karakters zijn");
        RuleFor(p => p.Description)
            .MaximumLength(500).WithMessage("Uitleg van het pakket mag maximaal 500 karakters zijn");
        RuleFor(p => p.Price)
            .GreaterThanOrEqualTo(0m).WithMessage("Prijs van het pakket mag niet negatief zijn");
        RuleFor(p => p.Slots)
            .NotEmpty().WithMessage("Een pakket heeft minstens één dagdeel nodig");
        RuleForEach(p => p.Slots).ChildRules(slot =>
        {
            slot.RuleFor(s => s.Date).Must(BeValidDate).WithMessage("Ongeldige datum (yyyy-MM-dd)");
            slot.RuleFor(s => s.PartOrder).GreaterThanOrEqualTo(0).WithMessage("Ongeldig dagdeel");
        });
        RuleFor(p => p.Slots)
            .Must(slots => slots.Select(s => (s.Date, s.PartOrder)).Distinct().Count() == slots.Count)
            .WithMessage("Een pakket bevat hetzelfde dagdeel meerdere keren")
            .When(p => p.Slots is { Count: > 0 });
    }

    private static bool BeValidDate(string? d) =>
        d is not null && DateOnly.TryParseExact(d, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
}
