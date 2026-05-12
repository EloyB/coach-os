namespace CoachOS.Application.SuperAdmin.DTOs;

public class AdminListItemDto
{
    public Guid UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    /// <summary>True zolang de admin de invite niet heeft geaccepteerd.</summary>
    public bool InvitePending { get; set; }

    public DateTime CreatedAt { get; set; }

    public List<AdminOrganizationDto> Organizations { get; set; } = [];
}

public class AdminOrganizationDto
{
    public Guid OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public bool IsEarlyBird { get; set; }

    /// <summary>De membership-actief vlag — false als de uitnodiging nog niet geaccepteerd is.</summary>
    public bool MembershipActive { get; set; }
}
