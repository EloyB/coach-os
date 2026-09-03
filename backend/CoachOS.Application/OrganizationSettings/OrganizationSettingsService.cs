using CoachOS.Application.Mappings;
using CoachOS.Application.OrganizationSettings.DTOs;
using CoachOS.Domain.Common;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;

namespace CoachOS.Application.OrganizationSettings;

public class OrganizationSettingsService(
    IOrganizationSettingsRepository repo,
    ILessonRepository lessonRepo,
    ApplicationMapper mapper,
    TimeProvider timeProvider) : IOrganizationSettingsService
{
    public async Task<Result<OrganizationSettingsDto>> GetAsync(
        Guid organizationId,
        Guid currentUserId,
        CancellationToken ct = default)
    {
        Domain.Entities.OrganizationSettings settings = await GetOrCreateAsync(organizationId, ct);
        int upcoming = await CountUpcomingForCurrentUserAsync(currentUserId, organizationId, ct);
        return Result<OrganizationSettingsDto>.Ok(mapper.ToOrganizationSettingsDto(settings, upcoming));
    }

    public async Task<Result<OrganizationSettingsDto>> UpdateAsync(
        Guid organizationId,
        Guid currentUserId,
        UpdateOrganizationSettingsRequest request,
        CancellationToken ct = default)
    {
        Domain.Entities.OrganizationSettings settings = await GetOrCreateAsync(organizationId, ct);
        settings.AdminsActAsTrainers = request.AdminsActAsTrainers;
        await repo.SaveChangesAsync(ct);

        int upcoming = await CountUpcomingForCurrentUserAsync(currentUserId, organizationId, ct);
        return Result<OrganizationSettingsDto>.Ok(mapper.ToOrganizationSettingsDto(settings, upcoming));
    }

    private async Task<Domain.Entities.OrganizationSettings> GetOrCreateAsync(Guid organizationId, CancellationToken ct)
    {
        Domain.Entities.OrganizationSettings? existing = await repo.GetByOrganizationAsync(organizationId, ct);
        if (existing is not null) return existing;

        Domain.Entities.OrganizationSettings created = new()
        {
            OrganizationId = organizationId,
            AdminsActAsTrainers = true,
        };
        await repo.AddAsync(created, ct);
        await repo.SaveChangesAsync(ct);
        return created;
    }

    private Task<int> CountUpcomingForCurrentUserAsync(Guid userId, Guid organizationId, CancellationToken ct)
    {
        DateOnly today = timeProvider.GetBrusselsToday();
        return lessonRepo.CountUpcomingForTrainerAsync(userId, organizationId, today, ct);
    }
}
