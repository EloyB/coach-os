using FluentValidation;

namespace CoachOS.Application.Trainers.Commands.InviteTrainer;

public class InviteTrainerCommandValidator : AbstractValidator<InviteTrainerCommand>
{
    public InviteTrainerCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Voornaam is verplicht")
            .MaximumLength(100).WithMessage("Voornaam mag maximaal 100 karakters zijn");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Achternaam is verplicht")
            .MaximumLength(100).WithMessage("Achternaam mag maximaal 100 karakters zijn");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-mail is verplicht")
            .EmailAddress().WithMessage("Ongeldig e-mailadres");

        RuleFor(x => x.OrganizationId)
            .NotEmpty().WithMessage("OrganizationId is verplicht");
    }
}
