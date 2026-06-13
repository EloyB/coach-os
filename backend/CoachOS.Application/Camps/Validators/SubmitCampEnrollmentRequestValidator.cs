using System.Text.RegularExpressions;
using CoachOS.Application.Camps.DTOs;
using FluentValidation;

namespace CoachOS.Application.Camps.Validators;

public class SubmitCampEnrollmentRequestValidator : AbstractValidator<SubmitCampEnrollmentRequest>
{
    private static readonly Regex EmailPattern = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public SubmitCampEnrollmentRequestValidator()
    {
        RuleFor(x => x.ParticipantName).NotEmpty().WithMessage("Naam is verplicht");
        RuleFor(x => x.ParticipantEmail)
            .NotEmpty().WithMessage("E-mailadres is verplicht")
            .Must(e => EmailPattern.IsMatch(e)).WithMessage("Ongeldig e-mailadres");

        RuleForEach(x => x.GroupMembers).ChildRules(member =>
        {
            member.RuleFor(m => m.ParticipantName).NotEmpty().WithMessage("Naam groepslid is verplicht");
            member.RuleFor(m => m.ParticipantEmail)
                .NotEmpty().WithMessage("E-mailadres groepslid is verplicht")
                .Must(e => EmailPattern.IsMatch(e)).WithMessage("Ongeldig e-mailadres groepslid");
        }).When(x => x.EnrollmentType == "group" && x.GroupMembers is not null);
    }
}
