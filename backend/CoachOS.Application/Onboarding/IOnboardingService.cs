using CoachOS.Application.Onboarding.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Onboarding;

public interface IOnboardingService
{
    /// <summary>
    /// Levert de huidige onboarding-state voor de organisatie. <c>ShouldShow</c> bepaalt of de FE
    /// de checklist überhaupt rendert; <c>AllCompleted</c> triggert de celebration variant.
    /// </summary>
    Task<Result<OnboardingStateDto>> GetStateAsync(Guid organizationId, CancellationToken ct = default);

    /// <summary>
    /// Markeert de onboarding als afgesloten (idempotent — re-aanroepen overschrijft de stamp niet).
    /// Daarna verschijnt de checklist niet meer terug, ook niet als er stappen incompleet zouden raken.
    /// </summary>
    Task<Result> DismissAsync(Guid organizationId, CancellationToken ct = default);

    /// <summary>
    /// Verwerkt de keuze uit het trainer-setup dialog. Zet <c>AdminsActAsTrainers</c> op de gegeven
    /// waarde en stempelt <c>TrainerModeChosenAt</c>. Geeft de verse state terug zodat de FE niet
    /// een aparte GET hoeft te doen.
    /// </summary>
    Task<Result<OnboardingStateDto>> SetTrainerModeAsync(
        Guid organizationId,
        SetTrainerModeRequest request,
        CancellationToken ct = default);
}
