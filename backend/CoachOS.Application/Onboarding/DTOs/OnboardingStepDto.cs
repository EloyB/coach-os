namespace CoachOS.Application.Onboarding.DTOs;

/// <summary>
/// Eén stap in de onboarding-checklist. <see cref="Key"/> is stabiel (mollie/club/trainerMode/series)
/// zodat de FE er gericht op kan mappen naar i18n-strings en deep-links.
/// </summary>
public record OnboardingStepDto(string Key, bool Completed);
