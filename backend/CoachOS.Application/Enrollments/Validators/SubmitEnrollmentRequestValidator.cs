using CoachOS.Application.Enrollments;
using CoachOS.Application.Enrollments.DTOs;
using CoachOS.Application.Common;
using FluentValidation;

namespace CoachOS.Application.Enrollments.Validators;

public class SubmitEnrollmentRequestValidator : AbstractValidator<SubmitEnrollmentRequest>
{
    public SubmitEnrollmentRequestValidator()
    {
        RuleFor(x => x.StudentName)
            .NotEmpty().WithMessage("Naam is verplicht")
            .MaximumLength(200).WithMessage("Naam mag maximaal 200 karakters zijn")
            .Must(InputSanitizer.IsFreeOfHtml).WithMessage("Naam mag geen HTML of scripttekens bevatten");

        RuleFor(x => x.StudentEmail)
            .NotEmpty().WithMessage("E-mailadres is verplicht")
            .MaximumLength(254).WithMessage("E-mailadres is te lang")
            .EmailAddress().WithMessage("Ongeldig e-mailadres");

        RuleFor(x => x.StudentPhone)
            .MaximumLength(30).WithMessage("Telefoonnummer is te lang")
            .Must(InputSanitizer.IsFreeOfHtmlNullable).WithMessage("Telefoonnummer mag geen HTML of scripttekens bevatten")
            .When(x => !string.IsNullOrEmpty(x.StudentPhone));

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
            // De unique index IX_Enrollments_LessonSerieId_StudentEmail laat hetzelfde
            // adres geen twee keer toe binnen één reeks. Vang dat hier af i.p.v. bij de insert.
            RuleFor(x => x)
                .Must(EnrollmentEmails.AreUnique)
                .WithMessage("Elk groepslid moet een uniek e-mailadres hebben");

            RuleFor(x => x.GroupMembers)
                .NotNull().WithMessage("Groepsleden zijn verplicht bij groepsinschrijving")
                .Must(m => m is { Count: > 0 and <= 3 })
                .WithMessage("Een groep heeft minimaal 1 en maximaal 3 extra leden");

            RuleForEach(x => x.GroupMembers).ChildRules(m =>
            {
                m.RuleFor(v => v.StudentName)
                    .NotEmpty().WithMessage("Naam is verplicht")
                    .MaximumLength(200).WithMessage("Naam mag maximaal 200 karakters zijn")
                    .Must(InputSanitizer.IsFreeOfHtml).WithMessage("Naam mag geen HTML of scripttekens bevatten");

                m.RuleFor(v => v.StudentEmail)
                    .NotEmpty().WithMessage("E-mailadres is verplicht")
                    .MaximumLength(254).WithMessage("E-mailadres is te lang")
                    .EmailAddress().WithMessage("Ongeldig e-mailadres");

                m.RuleFor(v => v.StudentPhone)
                    .MaximumLength(30).WithMessage("Telefoonnummer is te lang")
                    .Must(InputSanitizer.IsFreeOfHtmlNullable).WithMessage("Telefoonnummer mag geen HTML of scripttekens bevatten")
                    .When(v => !string.IsNullOrEmpty(v.StudentPhone));
            });
        });
    }
}
