namespace CoachOS.Application.Auth.DTOs;

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    /// <summary>De actieve organisatie in deze sessie (de org in de JWT claim).</summary>
    public Guid? OrganizationId { get; set; }

    /// <summary>Rol binnen de actieve organisatie.</summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>Alle (actieve) memberships van de user — nodig voor de org-switcher.</summary>
    public List<OrganizationMembershipDto> Memberships { get; set; } = [];

    /// <summary>
    /// True als deze sessie een system-level super-admin token is. In dat geval is
    /// <see cref="OrganizationId"/> null, <see cref="Role"/> = "SuperAdmin", en is
    /// <see cref="Memberships"/> leeg (Option C: super admins hebben geen memberships).
    /// </summary>
    public bool IsSuperAdmin { get; set; }
}
