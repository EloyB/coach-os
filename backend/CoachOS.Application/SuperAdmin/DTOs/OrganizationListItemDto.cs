namespace CoachOS.Application.SuperAdmin.DTOs;

public class OrganizationListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsEarlyBird { get; set; }
    public int AdminCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
