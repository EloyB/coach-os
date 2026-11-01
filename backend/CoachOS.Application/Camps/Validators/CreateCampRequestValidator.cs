using System.Globalization;
using CoachOS.Application.Camps.DTOs;
using FluentValidation;

namespace CoachOS.Application.Camps.Validators;

public class CreateCampRequestValidator : AbstractValidator<CreateCampRequest>
{
    private const string DateFormat = "yyyy-MM-dd";

    public CreateCampRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Naam is verplicht")
            .MaximumLength(200).WithMessage("Naam mag maximaal 200 karakters zijn");

        RuleFor(x => x.TennisClubId)
            .NotEmpty().WithMessage("Club is verplicht");

        RuleFor(x => x)
            .Custom((x, ctx) =>
            {
                string? error = CampRequestRules.PriceError(x.DayPrice, x.HalfDayPrice, x.AllowHalfDays);
                if (error is not null) ctx.AddFailure(error);
            });

        RuleFor(x => x.MaxParticipants)
            .GreaterThan(0).WithMessage("Maximum aantal deelnemers moet groter dan 0 zijn")
            .When(x => x.MaxParticipants.HasValue);

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

        RuleForEach(x => x.Days)
            .Custom((day, ctx) =>
            {
                foreach (string error in CampRequestRules.DayErrors(day))
                    ctx.AddFailure(error);
            });
    }

    private static bool BeValidDate(string? d) =>
        d is not null && DateOnly.TryParseExact(d, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
    private static DateOnly ParseDate(string d) => DateOnly.ParseExact(d, DateFormat, CultureInfo.InvariantCulture);
}
