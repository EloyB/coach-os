using FluentValidation;

namespace CoachOS.Application.TennisClubs.Commands.CreateTennisClub;

public class CreateTennisClubCommandValidator : AbstractValidator<CreateTennisClubCommand>
{
    public CreateTennisClubCommandValidator()
    {
        RuleFor(x => x.OrganizationId)
            .NotEmpty().WithMessage("OrganizationId is verplicht.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Naam is verplicht.")
            .MaximumLength(200).WithMessage("Naam mag maximaal 200 karakters zijn.");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Adres is verplicht.")
            .MaximumLength(500).WithMessage("Adres mag maximaal 500 karakters zijn.");
    }
}
