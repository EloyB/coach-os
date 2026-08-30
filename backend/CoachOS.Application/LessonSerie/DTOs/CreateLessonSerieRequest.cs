namespace CoachOS.Application.LessonSerie.DTOs;

public record CreateLessonSerieRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int? Level { get; init; }
    public decimal Price { get; init; }
    public string StartDate { get; init; } = string.Empty;
    public string EndDate { get; init; } = string.Empty;
    public DateTime RegistrationDeadline { get; init; }
    public int? MaxRegistrations { get; init; }
    public int MinAge { get; init; } = 3;
    public int MaxAge { get; init; } = 99;
    public Guid TennisClubId { get; init; }
    public bool AllowSoloEnrollment { get; init; } = true;
    public bool AllowGroupEnrollment { get; init; } = true;
    public bool AcceptOnlinePayment { get; init; } = true;
    public bool AcceptManualPayment { get; init; }
    public List<WeeklyTemplateEntryRequest> WeeklyTemplate { get; init; } = [];
    public List<CreateLessonRequest> Lessons { get; init; } = [];
}
