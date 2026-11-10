using CoachOS.Domain.Models;

namespace CoachOS.Application.Privacy;

public static class PlayerErasureErrors
{
    public static readonly Error NotFound =
        new(ErrorCodes.NotFound, "Inschrijving niet gevonden.");

    public static readonly Error Forbidden =
        new(ErrorCodes.Forbidden, "Alleen een actieve clubbeheerder kan spelers verwijderen.");

    public static readonly Error Blocked =
        new(ErrorCodes.Conflict, "Deze speler kan (nog) niet verwijderd worden. Open het overzicht opnieuw om te zien waarom.");

    public static readonly Error NameMismatch =
        new(ErrorCodes.Validation, "De ingevoerde naam komt niet exact overeen met de naam van de speler.");

    public static readonly Error ChangedSincePreview =
        new(ErrorCodes.Conflict, "De inschrijving is gewijzigd sinds het overzicht. Controleer het overzicht opnieuw.");

    public static readonly Error NewGroupLeaderRequired =
        new(ErrorCodes.Validation, "Kies een ander lid van dezelfde groep als nieuwe groepsleider.");

    public static readonly Error LockTimeout =
        new(ErrorCodes.Conflict, "Er loopt net een andere wijziging voor deze club of inschrijving. Probeer het over enkele seconden opnieuw.");

    public static readonly Error DependencyConflict =
        new(ErrorCodes.Conflict, "De inschrijving heeft nog gekoppelde gegevens die niet automatisch verwijderd kunnen worden. Er is niets gewist.");

    public static readonly Error RetentionNotDue =
        new(ErrorCodes.Conflict, "De bewaartermijn is nog niet verstreken.");
}
