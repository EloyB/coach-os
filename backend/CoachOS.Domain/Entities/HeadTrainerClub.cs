using CoachOS.Domain.Common;

namespace CoachOS.Domain.Entities;

/// <summary>
/// Grant: deze trainer (via z'n <see cref="OrganizationMembership"/>) is hoofdtrainer
/// van deze <see cref="TennisClub"/>. Geeft read-only elevatie (inschrijvingen + planning)
/// voor reeksen van die club. Meerdere rijen = hoofdtrainer van meerdere clubs.
/// </summary>
public class HeadTrainerClub : BaseEntity
{
    public Guid OrganizationMembershipId { get; set; }
    public Guid TennisClubId { get; set; }

    public OrganizationMembership Membership { get; set; } = null!;
    public TennisClub TennisClub { get; set; } = null!;
}
