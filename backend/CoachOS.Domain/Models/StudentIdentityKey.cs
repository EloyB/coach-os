namespace CoachOS.Domain.Models;

/// <summary>
/// Ruwe identiteitsvelden van een inschrijving, in de database al getrimd en naar kleine letters
/// omgezet. Enkel een projectie voor het tellen van unieke leerlingen.
/// </summary>
/// <param name="Name">Naam (trim + lowercase); meervoudige spaties worden pas in de service samengevouwen.</param>
/// <param name="DateOfBirth">Geboortedatum; null voor kampinschrijvingen en oude lesinschrijvingen.</param>
/// <param name="PrimaryEmail">Eigen adres van de deelnemer (trim + lowercase); kan null of leeg zijn.</param>
/// <param name="FallbackEmail">Contactadres, enkel gebruikt als <paramref name="PrimaryEmail"/> leeg is.</param>
public sealed record StudentIdentityKey(
    string Name, DateOnly? DateOfBirth, string? PrimaryEmail, string? FallbackEmail);
