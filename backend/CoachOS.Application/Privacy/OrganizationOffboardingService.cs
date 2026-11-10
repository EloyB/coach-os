using CoachOS.Application.Privacy.DTOs;
using CoachOS.Domain.Common;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using CoachOS.Domain.Subscriptions;
using Microsoft.Extensions.Logging;

namespace CoachOS.Application.Privacy;

public class OrganizationOffboardingService(
    IOrganizationRepository organizations,
    ISubscriptionRepository subscriptions,
    IOrganizationOffboardingRepository offboarding,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<OrganizationOffboardingService> logger) : IOrganizationOffboardingService
{
    public async Task<Result<OrganizationOffboardingPreviewDto>> GetPreviewAsync(
        Guid organizationId, CancellationToken ct = default)
    {
        Organization? organization = await organizations.GetByIdAsync(organizationId, tracked: false, ct);
        if (organization is null)
            return Result<OrganizationOffboardingPreviewDto>.Fail(NotFound());

        Subscription? subscription = await subscriptions.GetByOrganizationReadOnlyAsync(organizationId, ct);
        OrganizationDataCounts counts = await offboarding.CountDataAsync(organizationId, ct);
        IReadOnlyList<OffboardingBlockerDto> blockers =
            OffboardingBlockers.Evaluate(subscription, counts, timeProvider.GetBrusselsToday());

        return Result<OrganizationOffboardingPreviewDto>.Ok(new OrganizationOffboardingPreviewDto(
            organization.Id,
            organization.Name,
            (subscription?.Status ?? SubscriptionStatus.Legacy).ToString(),
            OrganizationRetentionClock.EndedOn(subscription),
            OrganizationRetentionClock.DeletableFrom(subscription),
            blockers.Count == 0,
            blockers,
            ToDto(counts)));
    }

    public async Task<Result<OrganizationDeletedDto>> DeleteAsync(
        Guid organizationId, DeleteOrganizationRequest request, CancellationToken ct = default)
    {
        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            // Eerst vergrendelen, dan pas lezen: zo beslissen we op de toestand na een
            // gelijktijdige wijziging (bv. een heractivatie) en niet op een verouderde kopie.
            await offboarding.LockOrganizationAsync(organizationId, ct);
            Result checks = await CheckDeletableAsync(organizationId, request, ct);
            if (!checks.IsSuccess)
            {
                await unitOfWork.RollbackTransactionAsync(ct);
                return Result<OrganizationDeletedDto>.Fail(checks.Errors);
            }

            int deletedAccounts = await offboarding.DeleteOrganizationAsync(organizationId, ct);
            await unitOfWork.CommitTransactionAsync(ct);

            DateTime erasedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
            // Bewijs van verwijdering zonder persoonsgegevens: enkel id, soort en tijdstip.
            logger.LogInformation(
                "Offboarding: organisatie {OrganizationId} verwijderd op {ErasedAtUtc:O} (Kind=Organization, {DeletedAccounts} accounts)",
                organizationId, erasedAtUtc, deletedAccounts);
            return Result<OrganizationDeletedDto>.Ok(new OrganizationDeletedDto(organizationId, deletedAccounts, erasedAtUtc));
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            throw;
        }
    }

    private async Task<Result> CheckDeletableAsync(
        Guid organizationId, DeleteOrganizationRequest request, CancellationToken ct)
    {
        Organization? organization = await organizations.GetByIdAsync(organizationId, tracked: false, ct);
        if (organization is null)
            return Result.Fail(NotFound());

        if (!string.Equals(request.ConfirmationName, organization.Name, StringComparison.Ordinal))
        {
            return Result.Fail(new Error(ErrorCodes.Validation,
                "De bevestigingsnaam komt niet exact overeen met de naam van de organisatie."));
        }

        Subscription? subscription = await subscriptions.GetByOrganizationReadOnlyAsync(organizationId, ct);
        OrganizationDataCounts counts = await offboarding.CountDataAsync(organizationId, ct);
        IReadOnlyList<OffboardingBlockerDto> blockers =
            OffboardingBlockers.Evaluate(subscription, counts, timeProvider.GetBrusselsToday());

        return blockers.Count == 0
            ? Result.Ok()
            : Result.Fail(blockers.Select(b => new Error(ErrorCodes.Conflict, b.Message)));
    }

    private static Error NotFound() => new(ErrorCodes.NotFound, "Organisatie niet gevonden.");

    private static OrganizationDataCountsDto ToDto(OrganizationDataCounts c) => new(
        c.TennisClubs, c.LessonSeries, c.Lessons, c.Enrollments, c.Camps, c.CampEnrollments,
        c.Payments, c.Memberships, c.AccountsToDelete, c.SubscriptionPayments);
}
