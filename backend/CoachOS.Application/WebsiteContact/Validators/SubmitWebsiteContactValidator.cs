using CoachOS.Application.WebsiteContact.DTOs;
using FluentValidation;

namespace CoachOS.Application.WebsiteContact.Validators;

public sealed class SubmitWebsiteContactValidator : AbstractValidator<SubmitWebsiteContactRequest>
{
    public SubmitWebsiteContactValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Naam is verplicht")
            .MinimumLength(2).WithMessage("Naam moet minstens 2 karakters zijn")
            .MaximumLength(100).WithMessage("Naam mag maximaal 100 karakters zijn");
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-mailadres is verplicht")
            .MaximumLength(254).WithMessage("E-mailadres mag maximaal 254 karakters zijn")
            .EmailAddress().WithMessage("Ongeldig e-mailadres");
        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Bericht is verplicht")
            .MinimumLength(10).WithMessage("Bericht moet minstens 10 karakters zijn")
            .MaximumLength(2000).WithMessage("Bericht mag maximaal 2000 karakters zijn");
        RuleFor(x => x.Organization)
            .MaximumLength(100).WithMessage("Organisatie mag maximaal 100 karakters zijn");
    }
}
