namespace CoachOS.Application.TennisClubs.DTOs;

public record CreateTennisClubRequest
{
    public string Name { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
}
