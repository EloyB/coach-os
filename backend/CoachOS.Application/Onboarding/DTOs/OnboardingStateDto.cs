namespace CoachOS.Application.Onboarding.DTOs;

/// <summary>
/// Volledige onboarding-state voor de huidige organisatie.
/// </summary>
/// <param name="ShouldShow">True wanneer er een actieve onboarding loopt
/// (<c>OnboardingStartedAt != null && OnboardingDismissedAt == null</c>).</param>
/// <param name="AllCompleted">True wanneer alle stappen voltooid zijn — FE rendert dan de celebration state.</param>
/// <param name="Steps">Volgorde-stabiele lijst van stappen.</param>
public record OnboardingStateDto(
    bool ShouldShow,
    bool AllCompleted,
    IReadOnlyList<OnboardingStepDto> Steps,
    DateTime? StartedAt,
    DateTime? DismissedAt);
