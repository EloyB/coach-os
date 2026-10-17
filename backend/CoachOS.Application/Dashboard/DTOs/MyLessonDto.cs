namespace CoachOS.Application.Dashboard.DTOs;

/// <summary>Een les van de ingelogde trainer, voor het trainersdashboard (vandaag + maandkalender).</summary>
public class MyLessonDto
{
    public Guid Id { get; set; }

    /// <summary>Null voor losse lessen.</summary>
    public Guid? SeriesId { get; set; }

    /// <summary>Reeksnaam; null voor losse lessen (de UI toont dan een vaste titel).</summary>
    public string? SeriesName { get; set; }

    public string Date { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string? CourtName { get; set; }
    public string? TennisClubName { get; set; }

    /// <summary>Aantal actieve inschrijvingen van de reeks; null voor losse lessen.</summary>
    public int? EnrolledCount { get; set; }
}

/// <summary>Volledige info over één eigen les, voor de popup op het trainersdashboard.</summary>
public class MyLessonDetailDto
{
    public Guid Id { get; set; }
    public Guid? SeriesId { get; set; }
    public string? SeriesName { get; set; }
    public string Date { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public string? CourtName { get; set; }
    public string? TennisClubName { get; set; }
    public string? TennisClubAddress { get; set; }

    /// <summary>1 = Beginner, 2 = Intermediate, 3 = Advanced; les-niveau, anders reeks-niveau.</summary>
    public int? Level { get; set; }

    public string? Notes { get; set; }
    public int MaxParticipants { get; set; }

    public List<MyLessonParticipantDto> Participants { get; set; } = [];
}

public class MyLessonParticipantDto
{
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// "confirmed" | "awaiting" | "proposed" (ingepland in dit weekslot),
    /// "accepted" | "pending" | "declined" (uitnodiging voor een losse les).
    /// </summary>
    public string Status { get; set; } = string.Empty;
}

/// <summary>Een kampdag waarvoor de trainer is ingepland, met het eigen tijdvenster van de trainer.</summary>
public class MyCampDayDto
{
    public Guid CampId { get; set; }
    public string CampName { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;

    /// <summary>Het venster van de trainer zelf (kan korter zijn dan de kampuren).</summary>
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;

    public string? TennisClubName { get; set; }
}

/// <summary>Samenvatting voor het trainersdashboard: volgende les en uren.</summary>
public class MyOverviewDto
{
    /// <summary>Eerstvolgende les die nog niet voorbij is; null als er niets gepland staat.</summary>
    public MyLessonDto? NextLesson { get; set; }

    /// <summary>Geplande uren (lessen + kampdagen) in de huidige week (ma–zo), op 1 decimaal.</summary>
    public double HoursThisWeek { get; set; }

    /// <summary>Geplande uren (lessen + kampdagen) in de huidige kalendermaand, op 1 decimaal.</summary>
    public double HoursThisMonth { get; set; }

    /// <summary>Wekelijkse capaciteit (uren) van de trainer; 0 = niet ingesteld.</summary>
    public int WeeklyCapacityHours { get; set; }
}
