using CoachOS.Application.Privacy.DTOs;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Privacy;

/// <summary>Termijnen voor het opruimen van technische records (zie <see cref="PrivacyCleanupService"/>).</summary>
public static class PrivacyCleanupPolicy
{
    /// <summary>
    /// Een verstuurde mail blijft 30 dagen staan, met ontvanger maar zonder inhoud, voor support
    /// ("heb ik die mail gekregen?"). Daarna gaat de hele rij weg.
    /// </summary>
    public const int CompletedMailRetentionDays = 30;

    /// <summary>
    /// Een definitief mislukte mail houdt 30 dagen zijn inhoud, om na te gaan wie welke mail niet
    /// kreeg. Daarna worden inhoud en ontvanger gewist; type, fout en pogingen blijven staan.
    /// </summary>
    public const int FailedMailContentRetentionDays = 30;

    /// <summary>Een inloglink kan na het verlopen niet meer gebruikt worden: meteen weg.</summary>
    public const int ExpiredMagicLinkGraceDays = 0;

    /// <summary>
    /// Een bevestigingslink kan na het verlopen niet meer gebruikt worden. 30 dagen marge zodat
    /// de planning recente reacties nog kan tonen. Beantwoorde, nog geldige links blijven staan:
    /// de planning leest er de bevestigingsstatus uit.
    /// </summary>
    public const int ExpiredConfirmationTokenGraceDays = 30;
}

public sealed class PrivacyCleanupService(
    IPrivacyCleanupRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider time) : IPrivacyCleanupService
{
    public async Task<Result<PrivacyCleanupReport>> RunAsync(bool dryRun, CancellationToken ct = default)
    {
        DateTime now = time.GetUtcNow().UtcDateTime;
        bool apply = !dryRun;

        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            int mails = await repository.CleanupCompletedMailAsync(
                now.AddDays(-PrivacyCleanupPolicy.CompletedMailRetentionDays), apply, ct);
            int failedMails = await repository.ScrubFailedMailAsync(
                now.AddDays(-PrivacyCleanupPolicy.FailedMailContentRetentionDays), apply, ct);
            int magicLinks = await repository.CleanupExpiredMagicLinksAsync(
                now.AddDays(-PrivacyCleanupPolicy.ExpiredMagicLinkGraceDays), apply, ct);
            int tokens = await repository.CleanupExpiredConfirmationTokensAsync(
                now.AddDays(-PrivacyCleanupPolicy.ExpiredConfirmationTokenGraceDays), apply, ct);
            await unitOfWork.CommitTransactionAsync(ct);
            return Result<PrivacyCleanupReport>.Ok(new PrivacyCleanupReport(mails, failedMails, magicLinks, tokens));
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            throw;
        }
    }
}
