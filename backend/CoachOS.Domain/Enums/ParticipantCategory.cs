namespace CoachOS.Domain.Enums;

/// <summary>
/// Tariefcategorie van een deelnemer. Wordt afgeleid uit de geboortedatum op de
/// inschrijving en de leeftijdsgrens die de organisatie instelt
/// (<c>OrganizationSettings.YouthMaxAge</c>).
/// </summary>
public enum ParticipantCategory
{
    Adult = 1,
    Youth = 2
}
