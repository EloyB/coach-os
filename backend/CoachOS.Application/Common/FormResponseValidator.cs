using CoachOS.Domain.Models;

namespace CoachOS.Application.Common;

/// <summary>
/// Valideert ingediende formulier-antwoorden tegen de velddefinitie. Gedeeld door
/// reeks- en kampinschrijving. Werkt op geprojecteerde tuples zodat het zowel
/// FormField als CampFormField ondersteunt zonder koppeling.
/// </summary>
public static class FormResponseValidator
{
    public static Error? Validate(
        IEnumerable<(Guid Id, bool IsRequired, string Label, IReadOnlyList<string>? AllowedValues)> fields,
        IEnumerable<(Guid FormFieldId, string Value)> responses)
    {
        List<(Guid Id, bool IsRequired, string Label, IReadOnlyList<string>? AllowedValues)> fieldList = fields.ToList();
        List<(Guid FormFieldId, string Value)> responseList = responses.ToList();

        HashSet<Guid> fieldIds = fieldList.Select(f => f.Id).ToHashSet();
        foreach ((Guid formFieldId, string _) in responseList)
        {
            if (!fieldIds.Contains(formFieldId))
                return new Error(ErrorCodes.Validation, "Ongeldig formulierveld.");
        }

        foreach ((Guid id, bool _, string label, _) in fieldList.Where(f => f.IsRequired))
        {
            bool hasResponse = responseList.Any(r => r.FormFieldId == id && !string.IsNullOrWhiteSpace(r.Value));
            if (!hasResponse)
                return new Error(ErrorCodes.Validation, $"Veld '{label}' is verplicht.");
        }

        // Choice fields (MultipleChoice / AgeCategory) must carry one of the configured options.
        // Without this, a forged response could invent a value the form never offered — which for
        // the age category would create a bucket that leaves compatible participants ungrouped.
        Dictionary<Guid, (IReadOnlyList<string> Values, string Label)> choiceFields = fieldList
            .Where(f => f.AllowedValues is { Count: > 0 })
            .ToDictionary(f => f.Id, f => ((IReadOnlyList<string>)f.AllowedValues!, f.Label));

        foreach ((Guid formFieldId, string value) in responseList)
        {
            if (string.IsNullOrWhiteSpace(value)) continue;
            if (choiceFields.TryGetValue(formFieldId, out (IReadOnlyList<string> Values, string Label) choice)
                && !choice.Values.Contains(value))
                return new Error(ErrorCodes.Validation, $"Ongeldige keuze voor veld '{choice.Label}'.");
        }

        return null;
    }
}
