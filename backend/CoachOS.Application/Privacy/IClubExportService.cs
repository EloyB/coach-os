using CoachOS.Application.Export;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Privacy;

public interface IClubExportService
{
    /// <summary>
    /// ZIP met alle gegevens van de eigen organisatie, enkel voor een actieve beheerder met een
    /// actief, bevestigd account.
    /// </summary>
    Task<Result<ExportFileDto>> ExportAsync(Guid organizationId, Guid userId, CancellationToken ct = default);
}
