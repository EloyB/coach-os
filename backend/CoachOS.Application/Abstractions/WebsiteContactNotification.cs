namespace CoachOS.Application.Abstractions;

/// <summary>Inhoud van de interne melding over een nieuw contactbericht van de website.</summary>
public sealed record WebsiteContactNotification(
    string Name, string Email, string? Organization, string Message, DateTime ReceivedAtUtc);
