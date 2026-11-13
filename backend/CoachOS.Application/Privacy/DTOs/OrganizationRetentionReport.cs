namespace CoachOS.Application.Privacy.DTOs;

/// <summary>
/// Resultaat van één run van de organisatieretentie. Enkel aantallen, nooit persoonsgegevens.
/// Bij een dry-run tellen de velden wat er zou gebeuren.
/// </summary>
/// <param name="FirstNotices">Eerste verwijdermail (30 dagen vooraf) verstuurd en verwijdering gepland.</param>
/// <param name="FinalNotices">Laatste verwijdermail (7 dagen vooraf) verstuurd, of opnieuw verstuurd.</param>
/// <param name="Deleted">Organisaties verwijderd.</param>
/// <param name="HandedToSuperAdmin">Betalende clubs overgeslagen en aan de superadmin gemeld.</param>
/// <param name="Cancelled">Geplande verwijderingen vervallen omdat de club verder ging.</param>
/// <param name="Failed">Organisaties waarvoor een stap mislukte; volgende run opnieuw.</param>
/// <param name="Skipped">Overgeslagen: verlopen proef met volledige toegang, of geen mailadres.</param>
public sealed record OrganizationRetentionReport(
    int FirstNotices,
    int FinalNotices,
    int Deleted,
    int HandedToSuperAdmin,
    int Cancelled,
    int Failed,
    int Skipped)
{
    public static OrganizationRetentionReport Empty { get; } = new(0, 0, 0, 0, 0, 0, 0);

    public bool HasActivity => FirstNotices + FinalNotices + Deleted + HandedToSuperAdmin + Cancelled + Failed > 0;
}
