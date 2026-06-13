using CoachOS.Domain.Common;

namespace CoachOS.Domain.Entities;

/// <summary>Eén dag van een kamp, met de kampuren die de deelnemer ziet.</summary>
public class CampDay : BaseEntity
{
    public Guid CampId { get; set; }
    public Guid OrganizationId { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    // Navigation
    public Camp Camp { get; set; } = null!;
    public ICollection<CampDayTrainer> TrainerAssignments { get; set; } = new List<CampDayTrainer>();
}
