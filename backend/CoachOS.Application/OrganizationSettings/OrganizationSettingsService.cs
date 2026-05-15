using CoachOS.Application.Mappings;
using CoachOS.Application.OrganizationSettings.DTOs;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;

namespace CoachOS.Application.OrganizationSettings;

public class OrganizationSettingsService(
    IOrganizationSettingsRepository repo,
    ApplicationMapper mapper) : IOrganizationSettingsService
{
    public async Task<Result<OrganizationSettingsDto>> GetAsync(Guid organizationId, CancellationToken ct = default)
    {
        Domain.Entities.OrganizationSettings settings = await GetOrCreateAsync(organizationId, ct);
        return Result<OrganizationSettingsDto>.Ok(mapper.ToOrganizationSettingsDto(settings));
    }

    public async Task<Result<OrganizationSettingsDto>> UpdateAsync(
        Guid organizationId,
        UpdateOrganizationSettingsRequest request,
        CancellationToken ct = default)
    {
        Domain.Entities.OrganizationSettings settings = await GetOrCreateAsync(organizationId, ct);
        settings.AdminsActAsTrainers = request.AdminsActAsTrainers;
        await repo.SaveChangesAsync(ct);
        return Result<OrganizationSettingsDto>.Ok(mapper.ToOrganizationSettingsDto(settings));
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
}
