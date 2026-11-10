using CoachOS.Application.Privacy.DTOs;
using FluentValidation;

namespace CoachOS.Application.Privacy.Validators;

public class ErasePlayerRequestValidator : AbstractValidator<ErasePlayerRequest>
{
    public ErasePlayerRequestValidator()
    {
        RuleFor(x => x.ConfirmationName)
            .NotEmpty().WithMessage("Typ de naam van de speler om te bevestigen")
            .MaximumLength(250).WithMessage("Naam mag maximaal 250 karakters zijn");

        RuleFor(x => x.NewGroupLeaderId)
            .NotEqual(Guid.Empty).WithMessage("Ongeldige nieuwe groepsleider")
            .When(x => x.NewGroupLeaderId.HasValue);
    }
}
