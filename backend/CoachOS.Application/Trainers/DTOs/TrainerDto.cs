namespace CoachOS.Application.Trainers.DTOs;

public class TrainerDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool InvitePending { get; set; }
    public int LessonSeriesCount { get; set; }
    public decimal CurrentWeekHoursBooked { get; set; }
    public int WeeklyCapacityHours { get; set; }
    public DateTime CreatedAt { get; set; }
}
