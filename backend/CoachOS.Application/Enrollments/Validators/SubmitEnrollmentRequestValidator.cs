using CoachOS.Application.Enrollments.DTOs;
using FluentValidation;

namespace CoachOS.Application.Enrollments.Validators;

public class SubmitEnrollmentRequestValidator : AbstractValidator<SubmitEnrollmentRequest>
{
    public SubmitEnrollmentRequestValidator()
    {
        RuleFor(x => x.StudentName)
            .NotEmpty().WithMessage("Naam is verplicht")
            .MaximumLength(200).WithMessage("Naam mag maximaal 200 karakters zijn");

        RuleFor(x => x.StudentEmail)
            .NotEmpty().WithMessage("E-mailadres is verplicht")
            .EmailAddress().WithMessage("Ongeldig e-mailadres");

        RuleForEach(x => x.Responses).ChildRules(r =>
        {
            r.RuleFor(v => v.FormFieldId)
                .NotEmpty().WithMessage("FormFieldId is verplicht");

            r.RuleFor(v => v.Value)
                .MaximumLength(1000).WithMessage("Antwoord mag maximaal 1000 karakters zijn");
        });

        RuleFor(x => x.EnrollmentType)
            .Must(t => t is "solo" or "group")
            .WithMessage("Inschrijvingstype moet 'solo' of 'group' zijn");

        RuleForEach(x => x.TimeSlotPreferences).ChildRules(p =>
        {
            p.RuleFor(v => v.WeeklyTemplateEntryId)
                .NotEmpty().WithMessage("WeeklyTemplateEntryId is verplicht");

            p.RuleFor(v => v.Preference)
                .InclusiveBetween(1, 3).WithMessage("Voorkeur moet 1, 2 of 3 zijn");
        });

        When(x => x.EnrollmentType == "group", () =>
        {
            RuleFor(x => x.GroupMembers)
                .NotNull().WithMessage("Groepsleden zijn verplicht bij groepsinschrijving")
                .Must(m => m is { Count: > 0 and <= 3 })
                .WithMessage("Een groep heeft minimaal 1 en maximaal 3 extra leden");

            RuleForEach(x => x.GroupMembers).ChildRules(m =>
            {
                m.RuleFor(v => v.StudentName)
                    .NotEmpty().WithMessage("Naam is verplicht")
                    .MaximumLength(200).WithMessage("Naam mag maximaal 200 karakters zijn");

                m.RuleFor(v => v.StudentEmail)
                    .NotEmpty().WithMessage("E-mailadres is verplicht")
                    .EmailAddress().WithMessage("Ongeldig e-mailadres");
            });
        });
    }
}
