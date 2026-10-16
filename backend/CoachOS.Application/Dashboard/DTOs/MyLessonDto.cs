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
