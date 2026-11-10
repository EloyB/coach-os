using CoachOS.Application.Privacy.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Privacy;

/// <summary>
/// Ruimt technische records met persoonsgegevens op die hun doel gediend hebben (verstuurde
/// mails, verlopen links). Raakt geen spelersdossiers. Met <c>dryRun</c> wordt enkel geteld.
/// </summary>
public interface IPrivacyCleanupService
{
    Task<Result<PrivacyCleanupReport>> RunAsync(bool dryRun, CancellationToken ct = default);
}
