using CoachOS.Application.Privacy.DTOs;
using FluentValidation;

namespace CoachOS.Application.Privacy.Validators;

public class DeleteOrganizationRequestValidator : AbstractValidator<DeleteOrganizationRequest>
{
    public const int MaxNameLength = 200;

    public DeleteOrganizationRequestValidator()
    {
        RuleFor(x => x.ConfirmationName)
            .NotEmpty().WithMessage("Typ de exacte naam van de organisatie ter bevestiging.")
            .MaximumLength(MaxNameLength).WithMessage($"De naam mag maximaal {MaxNameLength} tekens zijn.");
    }
}
