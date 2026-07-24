namespace CoachOS.Application.LessonSerie.DTOs;

public record UpdateLessonSerieRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int? Level { get; init; }
    public decimal Price { get; init; }
    public DateTime RegistrationDeadline { get; init; }
    public bool IsActive { get; init; }
    public int? MaxRegistrations { get; init; }
    public int MinAge { get; init; } = 3;
    public int MaxAge { get; init; } = 99;
    public Guid TennisClubId { get; init; }
    public bool AllowSoloEnrollment { get; init; } = true;
    public bool AllowGroupEnrollment { get; init; } = true;
    public bool AcceptOnlinePayment { get; init; } = true;
    public bool AcceptManualPayment { get; init; }
}
