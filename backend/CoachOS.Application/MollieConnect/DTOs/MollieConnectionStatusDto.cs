namespace CoachOS.Application.MollieConnect.DTOs;

/// <summary>
/// Status van de Mollie-koppeling voor een organisatie. Door de admin-UI getoond
/// in de settings-pagina. <see cref="MollieOrganizationName"/> is enkel gezet
/// wanneer <see cref="Connected"/> true is.
/// </summary>
public record MollieConnectionStatusDto(
    bool Connected,
    string? MollieOrganizationName,
    DateTime? ConnectedAt);
