namespace CoachOS.Application.LessonSeries.DTOs;

public class LessonSeriesDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid TrainerId { get; set; }
    public string TrainerName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Level { get; set; }
    public decimal Price { get; set; }
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public bool IsActive { get; set; }
    public Guid TennisClubId { get; set; }
    public string TennisClubName { get; set; } = string.Empty;
    public string TennisClubAddress { get; set; } = string.Empty;
    public int LessonCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<LessonDto> Lessons { get; set; } = [];
}
