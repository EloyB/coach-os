using CoachOS.Application.RescheduleRequests.DTOs;
using FluentValidation;

namespace CoachOS.Application.RescheduleRequests.Validators;

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
