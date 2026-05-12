using CoachOS.Application.SuperAdmin.DTOs;
using FluentValidation;

namespace CoachOS.Application.SuperAdmin.Validators;

public class CreateAdminRequestValidator : AbstractValidator<CreateAdminRequest>
{
    public CreateAdminRequestValidator()
    {
        RuleFor(x => x.OrganizationName)
            .NotEmpty().WithMessage("Naam organisatie is verplicht")
            .MaximumLength(200).WithMessage("Naam organisatie mag maximaal 200 karakters zijn");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Voornaam is verplicht")
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Achternaam is verplicht")
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-mailadres is verplicht")
            .EmailAddress().WithMessage("Ongeldig e-mailadres")
            .MaximumLength(256);
    }
}
