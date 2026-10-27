using System.Globalization;
using System.Text.RegularExpressions;
using CoachOS.Application.Camps.DTOs;
using FluentValidation;

namespace CoachOS.Application.Camps.Validators;

/// <summary>Validatie van de dagdelen van één kampdag (maximaal 2, geordend, niet overlappend, binnen de dag).</summary>
public class CampDayPartsValidator : AbstractValidator<CreateCampDayRequest>
{
    public const int MaxPartsPerDay = 2;
    private static readonly Regex TimePattern = new(@"^([01]\d|2[0-3]):[0-5]\d$", RegexOptions.Compiled);

    public CampDayPartsValidator()
    {
        RuleFor(d => d.Parts)
            .Must(parts => parts is null || parts.Count <= MaxPartsPerDay)
            .WithMessage($"Een dag heeft maximaal {MaxPartsPerDay} dagdelen");

        RuleForEach(d => d.Parts).ChildRules(part =>
        {
            part.RuleFor(p => p.Name)
                .NotEmpty().WithMessage("Naam van het dagdeel is verplicht")
                .MaximumLength(60).WithMessage("Naam van het dagdeel mag maximaal 60 karakters zijn");
            part.RuleFor(p => p.StartTime).Must(BeValidTime).WithMessage("Ongeldige starttijd voor dagdeel (HH:mm)");
            part.RuleFor(p => p.EndTime).Must(BeValidTime).WithMessage("Ongeldige eindtijd voor dagdeel (HH:mm)");
            part.RuleFor(p => p)
                .Must(p => string.Compare(p.EndTime, p.StartTime, StringComparison.Ordinal) > 0)
                .WithMessage("Eindtijd van het dagdeel moet na de starttijd zijn")
                .When(p => BeValidTime(p.StartTime) && BeValidTime(p.EndTime));
        }).When(d => d.Parts is { Count: <= MaxPartsPerDay });

        RuleFor(d => d.Parts)
            .Must(parts => !Overlap(parts!))
            .WithMessage("Dagdelen mogen niet overlappen")
            .When(d => d.Parts is { Count: > 1 } && d.Parts.All(p => BeValidTime(p.StartTime) && BeValidTime(p.EndTime)));

        RuleFor(d => d.Parts)
            .Must((d, parts) => parts!.All(p =>
                string.Compare(p.StartTime, d.StartTime, StringComparison.Ordinal) >= 0
                && string.Compare(p.EndTime, d.EndTime, StringComparison.Ordinal) <= 0))
            .WithMessage("Dagdelen moeten binnen de uren van de dag vallen")
            .When(d => d.Parts is { Count: > 0 }
                && BeValidTime(d.StartTime) && BeValidTime(d.EndTime)
                && d.Parts.All(p => BeValidTime(p.StartTime) && BeValidTime(p.EndTime)));
    }

    private static bool BeValidTime(string? t) => t is not null && TimePattern.IsMatch(t);

    private static bool Overlap(List<CreateCampDayPartRequest> parts)
    {
        List<CreateCampDayPartRequest> ordered = parts.OrderBy(p => p.StartTime, StringComparer.Ordinal).ToList();
        for (int i = 1; i < ordered.Count; i++)
        {
            if (string.Compare(ordered[i].StartTime, ordered[i - 1].EndTime, StringComparison.Ordinal) < 0)
                return true;
        }
        return false;
    }
}
