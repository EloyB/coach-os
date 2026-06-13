using System.Globalization;
using System.Text.RegularExpressions;
using CoachOS.Application.Camps.DTOs;
using FluentValidation;

namespace CoachOS.Application.Camps.Validators;

/// <summary>
/// Mirror van <see cref="CreateCampRequestValidator"/>: zelfde regels, maar voor
/// het Update-contract waar <c>IsActive</c> vrij is (geen aparte validatie nodig).
/// </summary>
public class UpdateCampRequestValidator : AbstractValidator<UpdateCampRequest>
{
    private static readonly Regex TimePattern = new(@"^([01]\d|2[0-3]):[0-5]\d$", RegexOptions.Compiled);
    private const string DateFormat = "yyyy-MM-dd";

    public UpdateCampRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Naam is verplicht")
            .MaximumLength(200).WithMessage("Naam mag maximaal 200 karakters zijn");

        RuleFor(x => x.TennisClubId)
            .NotEmpty().WithMessage("Club is verplicht");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0m).WithMessage("Prijs mag niet negatief zijn");

        RuleFor(x => x.StartDate)
            .Must(BeValidDate).WithMessage("Ongeldige startdatum (yyyy-MM-dd)");

        RuleFor(x => x.EndDate)
            .Must(BeValidDate).WithMessage("Ongeldige einddatum (yyyy-MM-dd)");

        RuleFor(x => x)
            .Must(x => ParseDate(x.EndDate) >= ParseDate(x.StartDate))
            .WithMessage("Einddatum moet op of na de startdatum liggen")
            .When(x => BeValidDate(x.StartDate) && BeValidDate(x.EndDate));

        RuleFor(x => x.Days)
            .NotEmpty().WithMessage("Een kamp heeft minstens één dag nodig");

        RuleForEach(x => x.Days).ChildRules(day =>
        {
            day.RuleFor(d => d.Date).Must(BeValidDate).WithMessage("Ongeldige datum (yyyy-MM-dd)");
            day.RuleFor(d => d.StartTime).Must(BeValidTime).WithMessage("Ongeldige starttijd (HH:mm)");
            day.RuleFor(d => d.EndTime).Must(BeValidTime).WithMessage("Ongeldige eindtijd (HH:mm)");
            day.RuleFor(d => d)
                .Must(d => string.Compare(d.EndTime, d.StartTime, StringComparison.Ordinal) > 0)
                .WithMessage("Eindtijd moet na starttijd zijn")
                .When(d => BeValidTime(d.StartTime) && BeValidTime(d.EndTime));

            day.RuleForEach(d => d.Trainers).Must((d, trainer) =>
                    string.Compare(trainer.StartTime, d.StartTime, StringComparison.Ordinal) >= 0
                    && string.Compare(trainer.EndTime, d.EndTime, StringComparison.Ordinal) <= 0
                    && string.Compare(trainer.EndTime, trainer.StartTime, StringComparison.Ordinal) > 0)
                .WithMessage("Trainer-uren moeten binnen de kampuren van die dag vallen")
                .When(d => BeValidTime(d.StartTime) && BeValidTime(d.EndTime));
        });
    }

    private static bool BeValidTime(string? t) => t is not null && TimePattern.IsMatch(t);
    private static bool BeValidDate(string? d) =>
        d is not null && DateOnly.TryParseExact(d, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
    private static DateOnly ParseDate(string d) => DateOnly.ParseExact(d, DateFormat, CultureInfo.InvariantCulture);
}
