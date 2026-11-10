using CoachOS.Domain.Enums;

namespace CoachOS.Domain.Entities;

/// <summary>
/// Minimaal, PII-vrij bewijs dat een record definitief gewist werd (geen naam, e-mail of
/// antwoorden). Bewust zonder FK naar de organisatie of het gewiste record, zodat het bewijs
/// het subject kan overleven. Wordt in dezelfde transactie als de verwijdering geschreven.
/// </summary>
public class PrivacyErasureReceipt
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public PrivacyErasureKind Kind { get; set; }

    /// <summary>Gewist of geanonimiseerd. Bestaande bewijzen zijn allemaal <see cref="PrivacyErasureMethod.Deleted"/>.</summary>
    public PrivacyErasureMethod Method { get; set; } = PrivacyErasureMethod.Deleted;

    /// <summary>Id van het gewiste record (een willekeurige GUID, geen persoonsgegeven).</summary>
    public Guid SubjectId { get; set; }

    public DateTime ErasedAt { get; set; }
}
