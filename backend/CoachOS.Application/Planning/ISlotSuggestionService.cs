using CoachOS.Application.Planning.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Planning;

public interface ISlotSuggestionService
{
    /// <summary>
    /// Leidt uit de vastgelegde trainerbeschikbaarheid af welke tijdvensters bruikbaar zijn
    /// voor deze club, en hoeveel banen er per venster parallel gepland kunnen worden.
    /// </summary>
    Task<Result<List<SlotSuggestionDto>>> SuggestSlotsAsync(
        Guid organizationId,
        Guid tennisClubId,
        CancellationToken ct = default);
}
