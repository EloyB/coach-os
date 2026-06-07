namespace CoachOS.Application.Trainers.DTOs;

public record UpdateTrainerRequest
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public int WeeklyCapacityHours { get; init; }
    public string? Notes { get; init; }
}
