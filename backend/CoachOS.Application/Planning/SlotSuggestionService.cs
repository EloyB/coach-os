using CoachOS.Application.Planning.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Planning;

/// <summary>
/// Bouwt slot-suggesties op uit de vastgelegde trainerbeschikbaarheid.
///
/// Algoritme (per weekdag):
/// 1. Filter beschikbaarheden op club (null = elke club) en actief.
/// 2. Verzamel alle start- en eindtijden als grenspunten en sorteer ze.
/// 3. Loop over elk paar opeenvolgende grenspunten; dat is een atomair venster
///    waarbinnen de set beschikbare trainers per definitie constant is.
/// 4. Voeg aangrenzende vensters met exact dezelfde trainerset samen, zodat een
///    trainer die 17:00-19:00 en 19:00-21:00 heeft één venster 17:00-21:00 oplevert.
/// 5. Gooi lege vensters weg en vensters korter dan <see cref="MinimumWindowMinutes"/>.
/// </summary>
public class SlotSuggestionService(
    ITrainerAvailabilityRepository repo,
    ITennisClubRepository clubRepo,
    IUserLookupService userLookup) : ISlotSuggestionService
{
    private const int MinimumWindowMinutes = 60;

    public async Task<Result<List<SlotSuggestionDto>>> SuggestSlotsAsync(
        Guid organizationId,
        Guid tennisClubId,
        CancellationToken ct = default)
    {
        bool clubExists = await clubRepo.ExistsAsync(tennisClubId, organizationId, ct);
        if (!clubExists)
            return Result<List<SlotSuggestionDto>>.Fail(new Error(ErrorCodes.NotFound, "Club niet gevonden"));

        IReadOnlyList<TrainerAvailability> all = await repo.GetByOrganizationAsync(organizationId, ct);

        // Repository filtert al op org + IsActive, maar we filteren defensief opnieuw:
        // GetByOrganizationAsync is een gedeelde query en mocks in tests kunnen ruimer zijn.
        List<TrainerAvailability> relevant = all
            .Where(a => a.OrganizationId == organizationId
                && a.IsActive
                && (a.TennisClubId is null || a.TennisClubId == tennisClubId))
            .ToList();

        List<Window> windows = relevant
            .GroupBy(a => a.DayOfWeek)
            .OrderBy(g => g.Key)
            .SelectMany(g => BuildWindows(g.Key, g.ToList()))
            .Where(w => (w.End - w.Start).TotalMinutes >= MinimumWindowMinutes)
            .ToList();

        Dictionary<Guid, string> names = await userLookup.GetUserNamesByIdsAsync(
            windows.SelectMany(w => w.TrainerIds).Distinct(), ct);

        List<SlotSuggestionDto> suggestions = windows
            .Select(w => ToDto(w, names))
            .ToList();

        return Result<List<SlotSuggestionDto>>.Ok(suggestions);
    }

    private static SlotSuggestionDto ToDto(Window window, Dictionary<Guid, string> names)
    {
        List<SuggestedTrainerDto> trainers = window.TrainerIds
            .Select(id => new SuggestedTrainerDto(id, names.TryGetValue(id, out string? name) ? name : string.Empty))
            .OrderBy(t => t.Name)
            .ToList();

        return new SlotSuggestionDto(
            window.DayOfWeek,
            window.Start.ToString("HH:mm"),
            window.End.ToString("HH:mm"),
            trainers.Count,
            trainers,
            trainers.Count);
    }

    /// <summary>
    /// Splitst de beschikbaarheden van één weekdag op de grenspunten in atomaire
    /// vensters en voegt aangrenzende vensters met dezelfde trainerset weer samen.
    /// </summary>
    private static List<Window> BuildWindows(int dayOfWeek, List<TrainerAvailability> availabilities)
    {
        List<TimeOnly> boundaries = availabilities
            .SelectMany(a => new[] { a.StartTime, a.EndTime })
            .Distinct()
            .OrderBy(t => t)
            .ToList();

        List<Window> merged = [];

        for (int i = 0; i < boundaries.Count - 1; i++)
        {
            TimeOnly start = boundaries[i];
            TimeOnly end = boundaries[i + 1];

            HashSet<Guid> trainerIds = availabilities
                .Where(a => a.StartTime <= start && a.EndTime >= end)
                .Select(a => a.TrainerId)
                .ToHashSet();

            if (trainerIds.Count == 0)
                continue;

            Window? previous = merged.Count > 0 ? merged[^1] : null;
            if (previous is not null && previous.End == start && previous.TrainerIds.SetEquals(trainerIds))
            {
                merged[^1] = previous with { End = end };
                continue;
            }

            merged.Add(new Window(dayOfWeek, start, end, trainerIds));
        }

        return merged;
    }

    private sealed record Window(int DayOfWeek, TimeOnly Start, TimeOnly End, HashSet<Guid> TrainerIds);
}
