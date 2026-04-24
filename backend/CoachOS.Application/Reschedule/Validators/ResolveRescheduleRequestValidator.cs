using CoachOS.Application.Reschedule.DTOs;
using FluentValidation;

namespace CoachOS.Application.Reschedule.Validators;

public class ResolveRescheduleRequestValidator : AbstractValidator<ResolveRescheduleRequest>
{
    public ResolveRescheduleRequestValidator()
    {
        RuleFor(x => x.State)
            .NotEmpty().WithMessage("Status is verplicht")
            .Must(s => s is "approved" or "declined")
            .WithMessage("Status moet 'approved' of 'declined' zijn");

        RuleFor(x => x.Note)
            .MaximumLength(1000).When(x => x.Note is not null);
    }
}
