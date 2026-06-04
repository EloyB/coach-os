using CoachOS.Application.Onboarding.DTOs;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Onboarding;

public class OnboardingService(
    IOrganizationSettingsRepository settingsRepo,
    IMollieConnectionRepository mollieRepo,
    ITennisClubRepository clubRepo,
    ILessonSerieRepository seriesRepo) : IOnboardingService
{
    private const string StepMollie = "mollie";
    private const string StepClub = "club";
    private const string StepTrainerMode = "trainerMode";
    private const string StepSeries = "series";

    public async Task<Result<OnboardingStateDto>> GetStateAsync(Guid organizationId, CancellationToken ct = default)
    {
        Domain.Entities.OrganizationSettings? settings =
            await settingsRepo.GetByOrganizationReadOnlyAsync(organizationId, ct);

        // Geen settings = org bestaat niet of is pre-onboarding (extreem zeldzaam). FE krijgt
        // ShouldShow=false zodat er niets gerenderd wordt. Veiliger dan een fout terug te geven
        // omdat /dashboard dit altijd polt en een 500 zou de pagina breken.
        if (settings is null)
        {
            return Result<OnboardingStateDto>.Ok(EmptyState());
        }

        bool mollieDone = await mollieRepo.GetByOrganizationReadOnlyAsync(organizationId, ct) is not null;
        bool clubDone = await clubRepo.AnyByOrganizationAsync(organizationId, ct);
        bool trainerModeDone = settings.TrainerModeChosenAt is not null;
        bool seriesDone = await seriesRepo.AnyByOrganizationAsync(organizationId, ct);

        var steps = new List<OnboardingStepDto>
        {
            new(StepMollie, mollieDone),
            new(StepClub, clubDone),
            new(StepTrainerMode, trainerModeDone),
            new(StepSeries, seriesDone),
        };

        bool allCompleted = mollieDone && clubDone && trainerModeDone && seriesDone;
        bool shouldShow = settings.OnboardingStartedAt is not null
                          && settings.OnboardingDismissedAt is null;

        return Result<OnboardingStateDto>.Ok(new OnboardingStateDto(
            ShouldShow: shouldShow,
            AllCompleted: allCompleted,
            Steps: steps,
            StartedAt: settings.OnboardingStartedAt,
            DismissedAt: settings.OnboardingDismissedAt));
    }

    public async Task<Result> DismissAsync(Guid organizationId, CancellationToken ct = default)
    {
        Domain.Entities.OrganizationSettings? settings =
            await settingsRepo.GetByOrganizationAsync(organizationId, ct);

        if (settings is null)
        {
            return Result.Fail("Organization settings ontbreken.");
        }

        // Idempotent: een tweede dismiss laat de eerste timestamp ongemoeid zodat audits kloppen.
        if (settings.OnboardingDismissedAt is null)
        {
            settings.OnboardingDismissedAt = DateTime.UtcNow;
            await settingsRepo.SaveChangesAsync(ct);
        }

        return Result.Ok();
    }

    public async Task<Result<OnboardingStateDto>> SetTrainerModeAsync(
        Guid organizationId,
        SetTrainerModeRequest request,
        CancellationToken ct = default)
    {
        Domain.Entities.OrganizationSettings? settings =
            await settingsRepo.GetByOrganizationAsync(organizationId, ct);

        if (settings is null)
        {
            return Result<OnboardingStateDto>.Fail("Organization settings ontbreken.");
        }

        settings.AdminsActAsTrainers = request.AdminActsAsTrainer;
        settings.TrainerModeChosenAt = DateTime.UtcNow;
        await settingsRepo.SaveChangesAsync(ct);

        return await GetStateAsync(organizationId, ct);
    }

    private static OnboardingStateDto EmptyState() => new(
        ShouldShow: false,
        AllCompleted: false,
        Steps:
        [
            new OnboardingStepDto(StepMollie, false),
            new OnboardingStepDto(StepClub, false),
            new OnboardingStepDto(StepTrainerMode, false),
            new OnboardingStepDto(StepSeries, false),
        ],
        StartedAt: null,
        DismissedAt: null);
}
