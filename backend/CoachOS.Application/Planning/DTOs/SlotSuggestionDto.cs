namespace CoachOS.Application.Planning.DTOs;

/// <summary>Trainer die tijdens het voorgestelde venster beschikbaar is.</summary>
public record SuggestedTrainerDto(Guid Id, string Name);

/// <summary>
/// Een tijdvenster waarin een vast aantal trainers gelijktijdig beschikbaar is.
/// Het aantal beschikbare trainers bepaalt hoeveel banen er parallel ingepland kunnen worden.
/// </summary>
public record SlotSuggestionDto(
    int DayOfWeek,
    string StartTime,
    string EndTime,
    int AvailableTrainerCount,
    List<SuggestedTrainerDto> Trainers,
    int SuggestedParallelSlots);
