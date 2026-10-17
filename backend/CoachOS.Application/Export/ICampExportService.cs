using CoachOS.Domain.Models;

namespace CoachOS.Application.Export;

public interface ICampExportService
{
    /// <summary>
    /// Bouwt een Excel-export (2 tabbladen: inschrijvingen, kampdagen) van één kamp
    /// binnen de organisatie.
    /// </summary>
    Task<Result<ExportFileDto>> ExportCampAsync(
        Guid campId, Guid organizationId, CancellationToken ct = default);
}
