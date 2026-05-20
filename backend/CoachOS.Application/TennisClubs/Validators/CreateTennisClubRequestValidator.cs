using CoachOS.Application.Common;
using CoachOS.Application.TennisClubs.DTOs;
using FluentValidation;

namespace CoachOS.Application.TennisClubs.Validators;

public class CreateTennisClubRequestValidator : AbstractValidator<CreateTennisClubRequest>
{
    public CreateTennisClubRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Naam is verplicht.")
            .MaximumLength(200).WithMessage("Naam mag maximaal 200 karakters zijn.")
            .Must(InputSanitizer.IsFreeOfHtml).WithMessage("Naam mag geen HTML of scripttekens bevatten.");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Adres is verplicht.")
            .MaximumLength(500).WithMessage("Adres mag maximaal 500 karakters zijn.")
            .Must(InputSanitizer.IsFreeOfHtml).WithMessage("Adres mag geen HTML of scripttekens bevatten.");
    }
}
