using CoachOS.Application.Common;
using CoachOS.Domain.Common;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Enrollments.Submit;

/// <summary>Eén voorwaarde voor een publieke inschrijving. Puur: geen database, geen klok.</summary>
internal interface ISubmissionRule
{
    /// <summary>De fout als de inschrijving niet mag, anders null.</summary>
    Error? Check(Submission submission);
}

internal static class SubmissionRules
{
    /// <summary>
    /// De volgorde is de voorrang tussen fouten: de deelnemer ziet de eerste die faalt.
    /// Een nieuwe regel is een klasse hieronder plus een plek in deze lijst.
    /// </summary>
    public static readonly IReadOnlyList<ISubmissionRule> All =
    [
        new FormResponsesRule(),
        new MemberFormResponsesRule(),
        new EnrollmentModeRule(),
        new AgeLimitRule(),
        new MinimumAvailableSlotsRule(),
        new MaxGroupSizeRule(),
    ];

    public static Error? FirstViolation(Submission submission)
        => All.Select(rule => rule.Check(submission)).FirstOrDefault(error => error is not null);
}

/// <summary>De antwoorden passen bij de velden van het formulier: verplichte ingevuld, geen onbekende.</summary>
internal sealed class FormResponsesRule : ISubmissionRule
{
    // Enkel de antwoorden van de leider: groepsleden sturen er vandaag geen mee (#411).
    public Error? Check(Submission submission)
        => submission.Form is null
            ? null
            : FormResponseValidator.Validate(
                submission.Form.Fields.Select(f => (f.Id, f.IsRequired, f.Label)),
                submission.Leader.Responses.Select(r => (r.FormFieldId, r.Value)));
}

/// <summary>
/// Elk groepslid beantwoordt dezelfde vragen als de leider (zelfde verplicht-regels, #411). Ook zonder formulier,
/// zodat een antwoord op een veld dat niet bestaat een nette fout geeft in plaats van een opslagfout. Bij een
/// solo-aanvraag zijn er geen leden en valt er niets te toetsen.
/// </summary>
internal sealed class MemberFormResponsesRule : ISubmissionRule
{
    public Error? Check(Submission submission)
    {
        var fields = (submission.Form?.Fields ?? []).Select(f => (f.Id, f.IsRequired, f.Label)).ToList();

        foreach (var member in submission.Members)
        {
            var error = FormResponseValidator.Validate(fields, member.Responses.Select(r => (r.FormFieldId, r.Value)));
            if (error is not null)
                return new Error(error.Code, $"{member.Name}: {error.Message}");
        }

        return null;
    }
}

/// <summary>De admin bepaalt per reeks of solo, groep of beide toegelaten zijn.</summary>
internal sealed class EnrollmentModeRule : ISubmissionRule
{
    public Error? Check(Submission submission)
    {
        if (submission.IsGroupRequest && !submission.Series.AllowGroupEnrollment)
            return new Error(ErrorCodes.Validation, "Inschrijven in groep is niet mogelijk voor deze lessenreeks.");
        if (!submission.IsGroupRequest && !submission.Series.AllowSoloEnrollment)
            return new Error(ErrorCodes.Validation, "Solo inschrijven is niet mogelijk voor deze lessenreeks.");
        return null;
    }
}

/// <summary>Elke deelnemer valt op de startdatum binnen de leeftijdsgrens van de reeks.</summary>
internal sealed class AgeLimitRule : ISubmissionRule
{
    public Error? Check(Submission submission)
        => EnrollmentAgeRules.CheckSeriesAgeLimit(
            submission.Series, submission.Participants.Select(p => (p.Name, p.DateOfBirth)));
}

/// <summary>
/// Minimum aantal tijdvakken waarop de deelnemer kan. Enkel unieke weekslots van deze reeks met voorkeur
/// Beschikbaar of Voorkeur tellen mee. De eis is begrensd door het aantal weekslots (zonder weekslots geen eis).
/// Alleen voor de publieke inschrijving; handmatige inschrijvingen door de admin zijn vrijgesteld.
/// </summary>
internal sealed class MinimumAvailableSlotsRule : ISubmissionRule
{
    public Error? Check(Submission submission)
    {
        var series = submission.Series;
        if (series.WeeklyTemplate.Count == 0)
            return null;

        var required = Math.Min(series.MinimumAvailableSlots, series.WeeklyTemplate.Count);
        var seriesSlotIds = series.WeeklyTemplate.Select(w => w.Id).ToHashSet();
        var available = (submission.Request.TimeSlotPreferences ?? [])
            .Where(p => (p.Preference == (int)SlotPreference.Available || p.Preference == (int)SlotPreference.Preferred)
                        && seriesSlotIds.Contains(p.WeeklyTemplateEntryId))
            .Select(p => p.WeeklyTemplateEntryId)
            .Distinct()
            .Count();

        return available >= required
            ? null
            : new Error(ErrorCodes.Validation,
                $"Kies minstens {required} tijdvak{(required == 1 ? "" : "ken")} waarop je kunt.");
    }
}

/// <summary>Het gekozen maximum aantal personen per tijdslot: tussen de eigen groepsgrootte en wat de reeks toelaat.</summary>
internal sealed class MaxGroupSizeRule : ISubmissionRule
{
    public Error? Check(Submission submission)
        => GroupSizeRules.Validate(
            Submission.ResolveMaxGroupSize(submission.Request),
            submission.Participants.Count,
            GroupSizeRules.SeriesMax(submission.Series.WeeklyTemplate.Select(w => w.MaxStudents)));
}
