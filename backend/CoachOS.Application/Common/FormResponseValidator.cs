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
        IEnumerable<(Guid Id, bool IsRequired, string Label)> fields,
        IEnumerable<(Guid FormFieldId, string Value)> responses)
    {
        List<(Guid Id, bool IsRequired, string Label)> fieldList = fields.ToList();
        List<(Guid FormFieldId, string Value)> responseList = responses.ToList();

        HashSet<Guid> fieldIds = fieldList.Select(f => f.Id).ToHashSet();
        foreach ((Guid formFieldId, string _) in responseList)
        {
            if (!fieldIds.Contains(formFieldId))
                return new Error(ErrorCodes.Validation, "Ongeldig formulierveld.");
        }

        foreach ((Guid id, bool isRequired, string label) in fieldList.Where(f => f.IsRequired))
        {
            bool hasResponse = responseList.Any(r => r.FormFieldId == id && !string.IsNullOrWhiteSpace(r.Value));
            if (!hasResponse)
                return new Error(ErrorCodes.Validation, $"Veld '{label}' is verplicht.");
        }

        return null;
    }
}
