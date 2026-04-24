namespace CoachOS.Application.Dashboard.DTOs;

public class DashboardMetricsDto
{
    public List<WeekMetricDto> Lessons { get; set; } = [];
    public List<WeekMetricDto> OccupancyPct { get; set; } = [];
    public List<WeekMetricDto> OutstandingCents { get; set; } = [];
}
