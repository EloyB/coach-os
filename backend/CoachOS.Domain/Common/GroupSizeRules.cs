using CoachOS.Domain.Models;

namespace CoachOS.Domain.Common;

/// <summary>
/// Regels voor het maximum aantal personen waarmee een deelnemer (of groep) in één tijdslot wil zitten
/// (<c>Enrollment.MaxGroupSize</c>): 1 = solo, 2 = duo, 3 = trio, 4 = groep van vier.
/// </summary>
public static class GroupSizeRules
{
    /// <summary>Hoogste keuze die een deelnemer kan maken.</summary>
    public const int AbsoluteMax = 4;

    /// <summary>
    /// Hoogste keuze voor een reeks: de grootste capaciteit van een weekslot, begrensd tot <see cref="AbsoluteMax"/>.
    /// Zonder weekslots (nog) of zonder bruikbare capaciteit is dat <see cref="AbsoluteMax"/>.
    /// </summary>
    public static int SeriesMax(IEnumerable<int> slotCapacities)
    {
        int largest = slotCapacities.DefaultIfEmpty(0).Max();
        return largest < 1 ? AbsoluteMax : Math.Min(AbsoluteMax, largest);
    }

    /// <summary>
    /// Controleert een gekozen maximum. De ondergrens is de eigen groepsgrootte (een groep van 2 kan geen
    /// maximum van 1 kiezen). De bovengrens is het maximum van de reeks, maar nooit lager dan de eigen groep:
    /// een bestaande groep mag niet geblokkeerd worden door een kleiner slot.
    /// </summary>
    public static Error? Validate(int maxGroupSize, int ownSize, int seriesMax)
    {
        if (maxGroupSize < 1 || maxGroupSize > AbsoluteMax)
            return new Error(ErrorCodes.Validation, $"Het maximum aantal personen moet tussen 1 en {AbsoluteMax} liggen.");

        if (maxGroupSize < ownSize)
            return new Error(ErrorCodes.Validation,
                $"Het maximum ({maxGroupSize}) kan niet lager zijn dan de eigen groepsgrootte ({ownSize}).");

        int upper = Math.Max(seriesMax, ownSize);
        if (maxGroupSize > upper)
            return new Error(ErrorCodes.Validation,
                $"Het maximum ({maxGroupSize}) is hoger dan wat deze reeks toelaat ({upper}).");

        return null;
    }
}
