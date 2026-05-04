using CoachOS.Application.StandaloneLessons.DTOs;
using FluentValidation;

namespace CoachOS.Application.StandaloneLessons.Validators;

public class AddInvitationsRequestValidator : AbstractValidator<AddInvitationsRequest>
{
    public AddInvitationsRequestValidator()
    {
        RuleFor(x => x.Emails)
            .NotNull().WithMessage("E-mails zijn verplicht.")
            .Must(emails => emails.Count > 0).WithMessage("Minstens één e-mailadres is verplicht.");

        RuleForEach(x => x.Emails)
            .NotEmpty().WithMessage("E-mailadres mag niet leeg zijn.")
            .EmailAddress().WithMessage("Ongeldig e-mailadres.")
            .MaximumLength(254).WithMessage("E-mailadres is te lang.");
    }
}
