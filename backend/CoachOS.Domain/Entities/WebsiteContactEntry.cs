using CoachOS.Domain.Common;

namespace CoachOS.Domain.Entities;

// Public marketing submission, deliberately outside club tenant data.
public sealed class WebsiteContactEntry : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Organization { get; set; }
    public string Message { get; set; } = string.Empty;
}
