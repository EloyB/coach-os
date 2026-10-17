using CoachOS.Domain.Models;

namespace CoachOS.Application.Subscriptions;

public interface ITrialReminderService
{
    /// <summary>
    /// Verstuurt voor elke lopende proef de herinnering die vandaag aan de beurt is
    /// (dag 20, 27 of 30). Idempotent: opnieuw draaien verstuurt niets dubbel.
    /// Geeft het aantal verstuurde mails terug.
    /// </summary>
    Task<Result<int>> SendDueRemindersAsync(CancellationToken ct = default);
}
