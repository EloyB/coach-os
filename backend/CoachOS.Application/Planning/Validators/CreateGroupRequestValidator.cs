using CoachOS.Application.Planning.DTOs;
using FluentValidation;

namespace CoachOS.Application.Planning.Validators;

public class CreateGroupRequestValidator : AbstractValidator<CreateGroupRequest>
{
    public CreateGroupRequestValidator()
    {
        RuleFor(x => x.EnrollmentIds)
            .NotEmpty().WithMessage("Minimaal één inschrijving is verplicht")
            .Must(ids => ids.Count >= 2 && ids.Count <= 4)
            .WithMessage("Een groep moet 2 tot 4 leden bevatten");
    }
}
