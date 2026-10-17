using CoachOS.Application.Auth.DTOs;
using CoachOS.Application.Common;
using FluentValidation;

namespace CoachOS.Application.Auth.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.OrganizationName)
            .NotEmpty().WithMessage("Naam organisatie is verplicht")
            .MaximumLength(200).WithMessage("Naam organisatie mag maximaal 200 karakters zijn")
            .Must(InputSanitizer.IsFreeOfHtml).WithMessage("Naam organisatie mag geen HTML of scripttekens bevatten");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Voornaam is verplicht")
            .MaximumLength(100).WithMessage("Voornaam mag maximaal 100 karakters zijn")
            .Must(InputSanitizer.IsFreeOfHtml).WithMessage("Voornaam mag geen HTML of scripttekens bevatten");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Achternaam is verplicht")
            .MaximumLength(100).WithMessage("Achternaam mag maximaal 100 karakters zijn")
            .Must(InputSanitizer.IsFreeOfHtml).WithMessage("Achternaam mag geen HTML of scripttekens bevatten");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-mailadres is verplicht")
            .EmailAddress().WithMessage("E-mailadres is ongeldig");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Wachtwoord is verplicht")
            .MinimumLength(8).WithMessage("Wachtwoord moet minimaal 8 karakters zijn")
            .Must(p => p.Any(char.IsUpper)).WithMessage("Wachtwoord moet minimaal 1 hoofdletter bevatten")
            .Must(p => p.Any(char.IsDigit)).WithMessage("Wachtwoord moet minimaal 1 cijfer bevatten");

        RuleFor(x => x.AcceptTerms)
            .Equal(true).WithMessage("Je moet akkoord gaan met de algemene voorwaarden");

        RuleFor(x => x.AcceptDataProcessingAgreement)
            .Equal(true).WithMessage("Je moet akkoord gaan met de verwerkersovereenkomst");

        // Bewust een vage melding: een bot hoeft niet te weten welk veld hem verraadde.
        RuleFor(x => x.Website)
            .Empty().WithMessage("Registratie mislukt. Probeer het opnieuw.");
    }
}
