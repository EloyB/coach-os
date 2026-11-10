using CoachOS.Domain.Common;

namespace CoachOS.Domain.Entities;

/// <summary>Platform website data before an organization exists; never tenant/customer account data.</summary>
public sealed class WebsiteWaitlistEntry : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string? Role { get; set; }
    public string UnsubscribeTokenHash { get; set; } = string.Empty;
}

/// <summary>Sensitive, bounded delivery payload, separate from the hash-only subscriber store.</summary>
public sealed class WebsiteWaitlistMail : BaseEntity
{
    public Guid EntryId { get; set; }
    public string Token { get; set; } = string.Empty;
    public int Attempts { get; set; }
    public DateTime AvailableAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(7);
}
