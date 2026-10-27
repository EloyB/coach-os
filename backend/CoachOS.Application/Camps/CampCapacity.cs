using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;

namespace CoachOS.Application.Camps;

/// <summary>
/// Limiet per dagdeel: een kamp met pakketten telt per (datum, dagdeel) hoeveel actieve deelnemers het dekken.
/// Een deelnemer zonder (of met een onbekend) pakket telt mee in elk dagdeel van het kamp. Kampen zonder
/// pakketten of zonder maximum worden hier nooit als vol beoordeeld; die houden de totaaltelling.
/// </summary>
public static class CampCapacity
{
    /// <summary>De dagdelen die een pakket dekt; zonder of met een onbekend pakket alle dagdelen van het kamp.</summary>
    public static IReadOnlyList<(DateOnly Date, int PartOrder)> SlotsOf(Camp camp, Guid? packageId)
    {
        CampPackage? package = packageId is { } id ? camp.Packages.FirstOrDefault(p => p.Id == id) : null;
        if (package is not null)
            return package.Slots.Select(s => (s.Date, s.PartOrder)).ToList();

        return camp.Days.SelectMany(d => d.Parts.Select(p => (d.Date, p.Order))).ToList();
    }

    /// <summary>True als bestaande plus nieuwe deelnemers in één dagdeel <see cref="Camp.MaxParticipants"/> overschrijden.</summary>
    public static bool IsFull(Camp camp, IEnumerable<Guid?> existingPackageIds, IEnumerable<Guid?> newPackageIds)
    {
        if (camp.MaxParticipants is not { } max || camp.Packages.Count == 0) return false;

        Dictionary<(DateOnly, int), int> counts = Coverage(camp, existingPackageIds.Concat(newPackageIds));
        return counts.Values.Any(count => count > max);
    }

    /// <summary>
    /// Of de nieuwe deelnemers (met hun pakket) het kamp over de limiet tillen: per dagdeel bij een kamp met
    /// pakketten, anders als totaal. <paramref name="exceptEnrollmentId"/> telt niet mee (pakket wijzigen).
    /// </summary>
    public static async Task<bool> IsFullAsync(
        ICampEnrollmentRepository enrollments, Camp camp, IReadOnlyList<Guid?> newPackageIds,
        CancellationToken ct, Guid? exceptEnrollmentId = null)
    {
        if (!camp.MaxParticipants.HasValue) return false;

        if (camp.Packages.Count > 0)
        {
            List<CampEnrollment> active = await enrollments.GetActiveByCampAsync(camp.Id, ct);
            return IsFull(
                camp, active.Where(e => e.Id != exceptEnrollmentId).Select(e => e.CampPackageId), newPackageIds);
        }

        return await enrollments.CountActiveByCampAsync(camp.Id, ct) + newPackageIds.Count > camp.MaxParticipants.Value;
    }

    /// <summary>True als één van de dagdelen van het pakket de limiet al bereikt heeft (volgende deelnemer past niet).</summary>
    public static bool IsPackageFull(Camp camp, CampPackage package, IEnumerable<Guid?> existingPackageIds)
    {
        if (camp.MaxParticipants is not { } max || camp.Packages.Count == 0) return false;

        Dictionary<(DateOnly, int), int> counts = Coverage(camp, existingPackageIds);
        return package.Slots.Any(s => counts.GetValueOrDefault((s.Date, s.PartOrder)) >= max);
    }

    private static Dictionary<(DateOnly, int), int> Coverage(Camp camp, IEnumerable<Guid?> packageIds)
    {
        Dictionary<(DateOnly, int), int> counts = new();
        foreach (Guid? packageId in packageIds)
            foreach ((DateOnly Date, int PartOrder) slot in SlotsOf(camp, packageId))
                counts[slot] = counts.GetValueOrDefault(slot) + 1;
        return counts;
    }
}
