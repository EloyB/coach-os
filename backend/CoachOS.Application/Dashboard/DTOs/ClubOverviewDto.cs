namespace CoachOS.Application.Dashboard.DTOs;

/// <summary>Clubcijfers en aandachtspunten voor de hoofdtrainer (zonder financiële gegevens).</summary>
public class ClubOverviewDto
{
    public int ActiveSeriesCount { get; set; }

    /// <summary>Gemiddelde bezetting (0-100) over de reeksen met een maximum; null als er geen zijn.</summary>
    public int? AverageOccupancyPercent { get; set; }

    public int AttentionCount { get; set; }
    public List<ClubSeriesDto> Series { get; set; } = [];
    public List<ClubAttentionItemDto> Attention { get; set; } = [];
}

public class ClubSeriesDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? TennisClubName { get; set; }
    public int Enrolled { get; set; }
    public int? MaxRegistrations { get; set; }
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public List<string> TrainerNames { get; set; } = [];
}

/// <summary>
/// Eén aandachtspunt. <c>Type</c>: <c>series_underbooked</c>, <c>confirmation_pending</c>,
/// <c>planning_stalled</c> of <c>lesson_without_trainer</c>.
/// </summary>
public class ClubAttentionItemDto
{
    public string Type { get; set; } = string.Empty;
    public Guid? SeriesId { get; set; }
    public string? SeriesName { get; set; }
    public string? TennisClubName { get; set; }

    /// <summary>Spelers die nog niet bevestigd hebben, per reeks gegroepeerd (confirmation_pending).</summary>
    public List<string> StudentNames { get; set; } = [];

    /// <summary>
    /// Resterende uren tot de eerstvolgende bevestigingslink verloopt; negatief als die al verlopen is
    /// (confirmation_pending).
    /// </summary>
    public int? HoursRemaining { get; set; }

    public int? Enrolled { get; set; }
    public int? MaxRegistrations { get; set; }

    /// <summary>Startdatum van de reeks (planning_stalled) of datum van de les (lesson_without_trainer).</summary>
    public string? Date { get; set; }
}
