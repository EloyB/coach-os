namespace CoachOS.Application.Trainers.DTOs;

public record SetHeadTrainerClubsRequest
{
    /// <summary>Clubs waarvan deze trainer hoofdtrainer wordt. Lege lijst = intrekken.</summary>
    public List<Guid> ClubIds { get; init; } = [];
}
