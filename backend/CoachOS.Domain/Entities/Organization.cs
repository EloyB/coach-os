using CoachOS.Domain.Common;

namespace CoachOS.Domain.Entities;

/// <summary>
/// Tennisschool of padelclub - de tenant in het multi-tenant systeem.
/// </summary>
public class Organization : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string Country { get; set; } = "BE";
    public bool IsActive { get; set; } = true;
    public string? LogoUrl { get; set; }

    /// <summary>
    /// Early-bird klant: krijgt een lifetime-discount op de subscription.
    /// Enkel toggle-baar door een super admin via het super-admin panel (#91).
    /// </summary>
    public bool IsEarlyBird { get; set; }

    // Navigation properties
    public ICollection<LessonSerie> LessonSeries { get; set; } = new List<LessonSerie>();
    public Subscription? Subscription { get; set; }
    public OrganizationSettings? Settings { get; set; }
}
