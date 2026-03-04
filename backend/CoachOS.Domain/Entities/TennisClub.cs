using CoachOS.Domain.Common;

namespace CoachOS.Domain.Entities;

public class TennisClub : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public Organization Organization { get; set; } = null!;
}
