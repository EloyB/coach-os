using CoachOS.Application.Common;
using CoachOS.Application.TennisClubs.DTOs;
using FluentValidation;

namespace CoachOS.Application.TennisClubs.Validators;

public class UpdateTennisClubRequestValidator : AbstractValidator<UpdateTennisClubRequest>
{
    public UpdateTennisClubRequestValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(200).WithMessage("Naam mag maximaal 200 karakters zijn.")
            .Must(InputSanitizer.IsFreeOfHtmlNullable).WithMessage("Naam mag geen HTML of scripttekens bevatten.")
            .When(x => x.Name is not null);

        RuleFor(x => x.Address)
            .MaximumLength(500).WithMessage("Adres mag maximaal 500 karakters zijn.")
            .Must(InputSanitizer.IsFreeOfHtmlNullable).WithMessage("Adres mag geen HTML of scripttekens bevatten.")
            .When(x => x.Address is not null);
    }
}
