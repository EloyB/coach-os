namespace CoachOS.Application.LessonSerie.DTOs;

public class LessonSerieDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? Level { get; set; }
    public decimal Price { get; set; }
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public DateTime RegistrationDeadline { get; set; }
    public bool IsActive { get; set; }
    public int? MaxRegistrations { get; set; }
    public int MinAge { get; set; }
    public int MaxAge { get; set; }
    public Guid TennisClubId { get; set; }
    public List<WeeklyTemplateEntryDto> WeeklyTemplate { get; set; } = [];
    public string TennisClubName { get; set; } = string.Empty;
    public string TennisClubAddress { get; set; } = string.Empty;
    public int LessonCount { get; set; }
    public int EnrolledCount { get; set; }
    public int TotalCapacity { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<LessonDto> Lessons { get; set; } = [];
}
