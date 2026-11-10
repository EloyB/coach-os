using CoachOS.Application.WebsiteWaitlist.DTOs;
using FluentValidation;

namespace CoachOS.Application.WebsiteWaitlist.Validators;

public sealed class RegisterWebsiteWaitlistValidator : AbstractValidator<RegisterWebsiteWaitlistRequest>
{
    private static readonly string[] AllowedRoles = ["club", "coach", "anders"];

    public RegisterWebsiteWaitlistValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-mailadres is verplicht")
            .MaximumLength(254).WithMessage("E-mailadres mag maximaal 254 karakters zijn")
            .EmailAddress().WithMessage("Ongeldig e-mailadres");
        RuleFor(x => x.Role)
            .Must(role => role is null || AllowedRoles.Contains(role)).WithMessage("Ongeldige rol");
    }
}

public sealed class UnsubscribeWebsiteWaitlistValidator : AbstractValidator<UnsubscribeWebsiteWaitlistRequest>
{
    public UnsubscribeWebsiteWaitlistValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Uitschrijflink is ongeldig")
            .Matches("\\A[a-f0-9]{64}\\z").WithMessage("Uitschrijflink is ongeldig");
    }
}
