namespace CoachOS.Application.Auth.DTOs;

/// <summary>Membership van een user in een organisatie — voor multi-org login en org-switcher.</summary>
public class OrganizationMembershipDto
{
    public Guid OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
