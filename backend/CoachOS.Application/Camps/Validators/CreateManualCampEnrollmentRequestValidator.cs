using System.Text.RegularExpressions;
using CoachOS.Application.Camps.DTOs;
using FluentValidation;

namespace CoachOS.Application.Camps.Validators;

public class CreateManualCampEnrollmentRequestValidator : AbstractValidator<CreateManualCampEnrollmentRequest>
{
    private static readonly Regex EmailPattern = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public CreateManualCampEnrollmentRequestValidator()
    {
        RuleFor(x => x.ParticipantName)
            .NotEmpty().WithMessage("Naam is verplicht")
            .MaximumLength(200).WithMessage("Naam mag maximaal 200 karakters zijn");
        RuleFor(x => x.ParticipantEmail)
            .NotEmpty().WithMessage("E-mailadres is verplicht")
            .Must(e => EmailPattern.IsMatch(e)).WithMessage("Ongeldig e-mailadres")
            .MaximumLength(320).WithMessage("E-mailadres mag maximaal 320 karakters zijn");
        RuleFor(x => x.ParticipantPhone)
            .MaximumLength(50).WithMessage("Telefoonnummer mag maximaal 50 karakters zijn");
    }
}
