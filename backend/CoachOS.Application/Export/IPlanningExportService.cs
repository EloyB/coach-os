using CoachOS.Domain.Models;

namespace CoachOS.Application.Export;

public interface IPlanningExportService
{
    /// <summary>
    /// Bouwt een Excel-export (3 tabbladen: inschrijvingen, lesmomenten, geplande
    /// lessen) voor de planning van één lessenreeks binnen de organisatie.
    /// </summary>
    Task<Result<ExportFileDto>> ExportSeriePlanningAsync(
        Guid serieId, Guid organizationId, CancellationToken ct = default);
}
