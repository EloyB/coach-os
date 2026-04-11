namespace CoachOS.Application.Dashboard.DTOs;

public class DashboardSummaryDto
{
    public int ActiveSeriesCount { get; set; }
    public int LessonsThisWeekCount { get; set; }
    public int TotalEnrollmentCount { get; set; }
    public int ActiveTrainerCount { get; set; }
    public int TennisClubCount { get; set; }
    public List<UpcomingLessonDto> UpcomingLessons { get; set; } = [];
}

public class UpcomingLessonDto
{
    public Guid Id { get; set; }
    public string SeriesName { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string? CourtName { get; set; }
    public string TrainerName { get; set; } = string.Empty;
}
