namespace CoachOS.Application.OrganizationSettings.DTOs;

/// <summary>
/// Org-instellingen plus enkele context-velden over de huidige gebruiker zodat
/// de FE waarschuwingen kan tonen bij het wijzigen (bv. nog openstaande lessen
/// vóór toggle-off).
/// </summary>
public record OrganizationSettingsDto(
    bool AdminsActAsTrainers,
    int CurrentUserUpcomingLessonsAsTrainer);
