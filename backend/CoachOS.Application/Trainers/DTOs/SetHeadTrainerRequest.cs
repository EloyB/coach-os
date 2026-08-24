namespace CoachOS.Application.Trainers.DTOs;

public record SetHeadTrainerRequest
{
    public bool IsHeadTrainer { get; init; }
}
