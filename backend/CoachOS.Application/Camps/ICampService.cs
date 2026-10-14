using CoachOS.Application.Camps.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Camps;

public interface ICampService
{
    Task<Result<List<CampDto>>> GetAllAsync(Guid organizationId, CancellationToken ct = default);
    Task<Result<CampDetailDto>> GetByIdAsync(Guid id, Guid organizationId, CancellationToken ct = default);
    Task<Result<Guid>> CreateAsync(Guid organizationId, CreateCampRequest request, CancellationToken ct = default);
    Task<Result> UpdateAsync(Guid id, Guid organizationId, UpdateCampRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, Guid organizationId, CancellationToken ct = default);
    Task<Result<Guid>> SaveFormAsync(Guid campId, Guid organizationId, SaveCampFormRequest request, CancellationToken ct = default);
    Task<Result<CampEnrollmentFormDto?>> GetFormAsync(Guid campId, CancellationToken ct = default);
    Task<Result<List<CampEnrollmentDto>>> GetEnrollmentsAsync(Guid campId, Guid organizationId, CancellationToken ct = default);

    /// <summary>
    /// Markeert de cash-betaling van een kamp-inschrijving als betaald (door admin/trainer),
    /// wat de inschrijving bevestigt. Controleert eerst of het kamp bij de organisatie hoort.
    /// </summary>
    Task<Result> MarkEnrollmentCashPaidAsync(
        Guid campId, Guid campEnrollmentId, Guid organizationId, CancellationToken ct = default);

    /// <summary>
    /// Annuleert een kamp-inschrijving (soft-cancel). De leider van een groep met nog
    /// actieve leden kan niet los geannuleerd worden: annuleer dan de hele groep.
    /// De plaats komt automatisch vrij (capaciteitstelling sluit geannuleerd uit).
    /// </summary>
    Task<Result> CancelEnrollmentAsync(
        Guid campId, Guid campEnrollmentId, Guid organizationId, CancellationToken ct = default);

    /// <summary>
    /// Past naam, e-mail, telefoon en formulierantwoorden van een kamp-inschrijving aan.
    /// Betaling, bedrag en status blijven ongemoeid; een geannuleerde inschrijving is niet bewerkbaar.
    /// </summary>
    Task<Result> UpdateEnrollmentAsync(
        Guid campId, Guid campEnrollmentId, Guid organizationId, UpdateCampEnrollmentRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Voegt een deelnemer toe aan een bestaande kampgroep (beheerder-actie). Het lid erft
    /// de status van de leider. Geweigerd zodra de groepsbetaling betaald is of online loopt;
    /// bij een openstaande cash-betaling wordt het bedrag bijgewerkt.
    /// </summary>
    Task<Result<Guid>> AddGroupMemberAsync(
        Guid campId, Guid groupId, Guid organizationId, AddCampGroupMemberRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Annuleert alle nog-actieve leden van een kampgroep atomair (één SaveChanges).
    /// </summary>
    Task<Result> CancelGroupAsync(
        Guid campId, Guid groupId, Guid organizationId, CancellationToken ct = default);
}
