namespace CoachOS.Application.Trainers.DTOs;

public record ReassignSeriesRequest
{
    public Guid ToTrainerId { get; init; }
}
