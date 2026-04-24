using CoachOS.Application.Reschedule.DTOs;
using FluentValidation;

namespace CoachOS.Application.Reschedule.Validators;

public class CreateRescheduleRequestValidator : AbstractValidator<CreateRescheduleRequest>
{
    public CreateRescheduleRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reden is verplicht")
            .MaximumLength(1000).WithMessage("Reden mag maximaal 1000 tekens zijn");
    }
}
