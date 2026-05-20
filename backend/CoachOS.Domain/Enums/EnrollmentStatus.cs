namespace CoachOS.Domain.Enums;

public enum EnrollmentStatus
{
    Pending = 1,
    Confirmed = 2,
    Cancelled = 3,
    Waitlisted = 4,

    /// <summary>
    /// Inschrijving is geregistreerd maar wacht op succesvolle Mollie-betaling.
    /// Wordt <see cref="Confirmed"/> zodra de webhook een geslaagde betaling bevestigt.
    /// </summary>
    PendingPayment = 5
}
