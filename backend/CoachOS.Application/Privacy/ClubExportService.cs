using System.Data;
using CoachOS.Application.Export;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using Microsoft.Extensions.Logging;

namespace CoachOS.Application.Privacy;

public class ClubExportService(
    IOrganizationExportRepository exports,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<ClubExportService> logger) : IClubExportService
{
    public const string FileName = "coachos-club-export.zip";
    public const string ContentType = "application/zip";

    public async Task<Result<ExportFileDto>> ExportAsync(Guid organizationId, Guid userId, CancellationToken ct = default)
    {
        if (!await exports.IsActiveConfirmedAdminAsync(organizationId, userId, ct))
        {
            return Result<ExportFileDto>.Fail(new Error(ErrorCodes.Forbidden,
                "Enkel een actieve beheerder met een bevestigd account kan de clubgegevens exporteren."));
        }

        // Eén snapshot voor alle tabellen, zodat de export intern consistent is.
        await unitOfWork.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
        IReadOnlyList<ExportTable> tables;
        try
        {
            tables = await exports.GetTablesAsync(organizationId, ct);
            await unitOfWork.CommitTransactionAsync(ct);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            throw;
        }

        byte[] zip = ClubExportArchive.Build(organizationId, tables, timeProvider.GetUtcNow().UtcDateTime);
        logger.LogInformation("Clubexport voor organisatie {OrganizationId} door gebruiker {UserId}", organizationId, userId);
        return Result<ExportFileDto>.Ok(new ExportFileDto(zip, FileName, ContentType));
    }
}
