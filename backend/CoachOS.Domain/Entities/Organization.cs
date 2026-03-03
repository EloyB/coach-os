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

    // Navigation properties
    public ICollection<Court> Courts { get; set; } = new List<Court>();
    public ICollection<LessonSeries> LessonSeries { get; set; } = new List<LessonSeries>();
    public Subscription? Subscription { get; set; }
}
