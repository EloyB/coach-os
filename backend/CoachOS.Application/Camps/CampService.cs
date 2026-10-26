using CoachOS.Application.Abstractions;
using System.Data;
using System.Globalization;
using System.Text.Json;
using CoachOS.Application.Camps.DTOs;
using CoachOS.Application.Common;
using CoachOS.Application.Payments;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Camps;

public class CampService(
    ICampRepository camps,
    ICampEnrollmentRepository enrollments,
    ICampEnrollmentFormRepository forms,
    ITennisClubRepository clubs,
    IUserLookupService users,
    IPaymentRepository payments,
    IUnitOfWork unitOfWork,
    IPaymentService paymentService) : ICampService
{
    private const string DateFormat = "yyyy-MM-dd";

    public async Task<Result<List<CampDto>>> GetAllAsync(Guid organizationId, CancellationToken ct = default)
    {
        IReadOnlyList<Camp> list = await camps.GetByOrganizationAsync(organizationId, ct);
        Dictionary<Guid, int> counts = await enrollments.CountActiveByCampsAsync(list.Select(c => c.Id), ct);
        List<CampDto> dtos = list.Select(c => new CampDto(
            c.Id, c.Name, c.TennisClubId, c.TennisClub?.Name ?? string.Empty,
            c.Level.HasValue ? (int)c.Level.Value : null, c.Price,
            c.StartDate.ToString(DateFormat), c.EndDate.ToString(DateFormat),
            c.MaxParticipants, counts.GetValueOrDefault(c.Id, 0), c.Days.Count, c.IsActive)).ToList();
        return Result<List<CampDto>>.Ok(dtos);
    }

    public async Task<Result<CampDetailDto>> GetByIdAsync(Guid id, Guid organizationId, CancellationToken ct = default)
    {
        Camp? camp = await camps.GetByIdWithDetailsAsync(id, organizationId, ct);
        if (camp is null)
            return Result<CampDetailDto>.Fail(new Error(ErrorCodes.NotFound, "Kamp niet gevonden."));

        int participants = await enrollments.CountActiveByCampAsync(camp.Id, ct);
        List<CampDayDto> days = await BuildDayDtosAsync(camp, organizationId, ct);

        return Result<CampDetailDto>.Ok(new CampDetailDto(
            camp.Id, camp.Name, camp.Description, camp.TennisClubId, camp.TennisClub?.Name ?? string.Empty,
            camp.Level.HasValue ? (int)camp.Level.Value : null, camp.Price,
            camp.StartDate.ToString(DateFormat), camp.EndDate.ToString(DateFormat),
            DateTime.SpecifyKind(camp.RegistrationDeadline, DateTimeKind.Utc),
            camp.MaxParticipants, participants, camp.IsActive, days, MapPrices(camp), MapPackages(camp)));
    }

    public async Task<Result<Guid>> CreateAsync(Guid organizationId, CreateCampRequest request, CancellationToken ct = default)
    {
        Error? validation = await ValidateClubAndTrainersAsync(organizationId, request.TennisClubId, request.Days, ct);
        if (validation is not null) return Result<Guid>.Fail(validation);

        Error? slotError = ValidatePackageSlots(request.Days, request.Packages);
        if (slotError is not null) return Result<Guid>.Fail(slotError);

        Camp camp = BuildCamp(organizationId, request);
        await camps.AddAsync(camp, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result<Guid>.Ok(camp.Id);
    }

    public async Task<Result> UpdateAsync(Guid id, Guid organizationId, UpdateCampRequest request, CancellationToken ct = default)
    {
        Camp? camp = await camps.GetByIdWithDetailsAsync(id, organizationId, ct);
        if (camp is null) return Result.Fail(new Error(ErrorCodes.NotFound, "Kamp niet gevonden."));

        Error? validation = await ValidateClubAndTrainersAsync(organizationId, request.TennisClubId, request.Days, ct);
        if (validation is not null) return Result.Fail(validation);

        // Zonder meegestuurde pakketten blijven de bestaande staan: hun dagdelen moeten in de nieuwe dagen passen.
        List<SaveCampPackageRequest>? packagesToCheck = request.Packages ?? camp.Packages
            .Select(p => new SaveCampPackageRequest(p.Id, p.Name, p.Description, p.Price,
                p.Slots.Select(s => new CampPackageSlotDto(s.Date.ToString(DateFormat), s.PartOrder)).ToList()))
            .ToList();
        Error? slotError = ValidatePackageSlots(request.Days, packagesToCheck);
        if (slotError is not null) return Result.Fail(slotError);

        camp.Name = request.Name;
        camp.Description = request.Description;
        camp.TennisClubId = request.TennisClubId;
        camp.Level = request.Level.HasValue ? (LessonLevel)request.Level.Value : null;
        decimal previousPrice = camp.Price;
        camp.Price = request.Price;
        camp.StartDate = ParseDate(request.StartDate);
        camp.EndDate = ParseDate(request.EndDate);
        camp.RegistrationDeadline = DateTime.SpecifyKind(request.RegistrationDeadline, DateTimeKind.Utc);
        camp.MaxParticipants = request.MaxParticipants;
        camp.IsActive = request.IsActive;

        // Volledige vervanging van dagen + trainers (simpel; geen diff).
        // Restrict FK: oude dagen/trainers expliciet als Deleted markeren — geen auto-cascade.
        camps.RemoveDays(camp.Days.ToList());
        camp.Days.Clear();
        foreach (CampDay day in BuildDays(organizationId, request.Days))
            camp.Days.Add(day);

        bool pricesChanged = previousPrice != request.Price;
        if (request.Prices is not null)
        {
            Dictionary<Guid, decimal> previousAmounts = camp.Prices.ToDictionary(p => p.Id, p => p.Discount);
            Error? priceError = await SyncPricesAsync(camp, organizationId, request.Prices, ct);
            if (priceError is not null) return Result.Fail(priceError);
            pricesChanged |= camp.Prices.Any(p => previousAmounts.TryGetValue(p.Id, out decimal old) && old != p.Discount);
        }

        if (request.Packages is not null)
        {
            Dictionary<Guid, decimal> previousPackagePrices = camp.Packages.ToDictionary(p => p.Id, p => p.Price);
            Error? packageError = await SyncPackagesAsync(camp, organizationId, request.Packages, ct);
            if (packageError is not null) return Result.Fail(packageError);
            pricesChanged |= camp.Packages.Any(p =>
                previousPackagePrices.TryGetValue(p.Id, out decimal old) && old != p.Price);
        }

        // Openstaande cash-betalingen volgen een gewijzigde hoofdprijs, pakketprijs of tariefkorting.
        if (pricesChanged)
            await SyncAllPendingCashAmountsAsync(camp, organizationId, ct);

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> DeleteAsync(Guid id, Guid organizationId, CancellationToken ct = default)
    {
        Camp? camp = await camps.GetByIdWithDetailsAsync(id, organizationId, ct);
        if (camp is null) return Result.Fail(new Error(ErrorCodes.NotFound, "Kamp niet gevonden."));
        camp.IsActive = false;
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result<Guid>> SaveFormAsync(Guid campId, Guid organizationId, SaveCampFormRequest request, CancellationToken ct = default)
    {
        bool exists = await camps.ExistsAsync(campId, organizationId, ct);
        if (!exists) return Result<Guid>.Fail(new Error(ErrorCodes.NotFound, "Kamp niet gevonden."));

        CampEnrollmentForm? form = await forms.GetByCampIdWithFieldsAsync(campId, ct);
        if (form is null)
        {
            form = new CampEnrollmentForm { OrganizationId = organizationId, CampId = campId };
            await forms.AddAsync(form, ct);
        }

        List<Guid> incomingIds = request.Fields.Where(f => f.Id.HasValue).Select(f => f.Id!.Value).ToList();
        foreach (CampFormField field in form.Fields.Where(f => !incomingIds.Contains(f.Id)).ToList())
            forms.RemoveField(field);

        int order = 0;
        foreach (SaveCampFormFieldRequest dto in request.Fields)
        {
            string? optionsJson = dto.Type == (int)FormFieldType.MultipleChoice && dto.Options?.Count > 0
                ? JsonSerializer.Serialize(dto.Options)
                : null;

            if (dto.Id.HasValue)
            {
                CampFormField? existing = form.Fields.FirstOrDefault(f => f.Id == dto.Id.Value);
                if (existing is not null)
                {
                    existing.Label = dto.Label;
                    existing.Type = (FormFieldType)dto.Type;
                    existing.IsRequired = dto.IsRequired;
                    existing.Order = order;
                    existing.Options = optionsJson;
                }
            }
            else
            {
                form.Fields.Add(new CampFormField
                {
                    CampEnrollmentFormId = form.Id,
                    Label = dto.Label,
                    Type = (FormFieldType)dto.Type,
                    IsRequired = dto.IsRequired,
                    Order = order,
                    Options = optionsJson,
                });
            }
            order++;
        }

        await unitOfWork.SaveChangesAsync(ct);
        return Result<Guid>.Ok(form.Id);
    }

    public async Task<Result<CampEnrollmentFormDto?>> GetFormAsync(Guid campId, CancellationToken ct = default)
    {
        CampEnrollmentForm? form = await forms.GetByCampIdReadOnlyAsync(campId, ct);
        if (form is null) return Result<CampEnrollmentFormDto?>.Ok(null);

        return Result<CampEnrollmentFormDto?>.Ok(new CampEnrollmentFormDto
        {
            Id = form.Id,
            CampId = form.CampId,
            Fields = form.Fields.OrderBy(f => f.Order).Select(f => new CampFormFieldDto
            {
                Id = f.Id,
                Label = f.Label,
                Type = (int)f.Type,
                IsRequired = f.IsRequired,
                Order = f.Order,
                Options = DeserializeOptions(f.Options),
            }).ToList(),
        });
    }

    public async Task<Result<List<CampEnrollmentDto>>> GetEnrollmentsAsync(Guid campId, Guid organizationId, CancellationToken ct = default)
    {
        bool exists = await camps.ExistsAsync(campId, organizationId, ct);
        if (!exists) return Result<List<CampEnrollmentDto>>.Fail(new Error(ErrorCodes.NotFound, "Kamp niet gevonden."));

        List<CampEnrollment> rows = await enrollments.GetByCampWithResponsesAsync(campId, organizationId, ct);

        Dictionary<Guid, Payment> latestPayments =
            await payments.GetLatestByCampEnrollmentIdsAsync(rows.Select(e => e.Id), ct);

        List<CampEnrollmentDto> dtos = rows.Select(e =>
        {
            Payment? payment = latestPayments.GetValueOrDefault(e.Id);
            return new CampEnrollmentDto(
                e.Id, e.ParticipantName, e.ParticipantEmail, e.ParticipantPhone,
                e.Status.ToString(), e.EnrolledAt, e.Group?.Name,
                e.CampEnrollmentGroupId, e.Group?.LeaderEnrollmentId == e.Id,
                e.FormResponses.Select(r => new CampEnrollmentResponseItemDto(
                    r.CampFormFieldId, r.CampFormField?.Label ?? string.Empty, r.Value)).ToList(),
                payment?.Method?.ToString(), payment?.Status.ToString(),
                payment?.Amount, payment?.PaidAt, payment?.MolliePaymentId,
                e.CampPriceId, e.Price?.Label);
        }).ToList();
        return Result<List<CampEnrollmentDto>>.Ok(dtos);
    }

    public async Task<Result> MarkEnrollmentCashPaidAsync(
        Guid campId, Guid campEnrollmentId, Guid organizationId, CancellationToken ct = default)
    {
        bool exists = await camps.ExistsAsync(campId, organizationId, ct);
        if (!exists) return Result.Fail(new Error(ErrorCodes.NotFound, "Kamp niet gevonden."));

        return await paymentService.MarkCampCashPaidAsync(campId, campEnrollmentId, organizationId, ct);
    }

    public async Task<Result> CancelEnrollmentAsync(
        Guid campId, Guid campEnrollmentId, Guid organizationId, CancellationToken ct = default)
    {
        Camp? camp = await camps.GetByIdWithDetailsAsync(campId, organizationId, ct);
        if (camp is null) return Result.Fail(new Error(ErrorCodes.NotFound, "Kamp niet gevonden."));

        // GetByIdAsync filtert op organizationId (autorisatiecheck); CampId-check voorkomt
        // een cross-kamp annulering binnen dezelfde organisatie.
        CampEnrollment? enrollment = await enrollments.GetByIdAsync(campEnrollmentId, organizationId, ct);
        if (enrollment is null || enrollment.CampId != campId)
            return Result.Fail(new Error(ErrorCodes.NotFound, "Inschrijving niet gevonden."));

        if (enrollment.Status == EnrollmentStatus.Cancelled)
            return Result.Fail(new Error(ErrorCodes.Validation, "Deze inschrijving is al geannuleerd."));

        // De betaling hangt aan de leider (prijs × leden): een leider alleen annuleren
        // zou de overige leden zonder betaler achterlaten.
        CampEnrollmentGroup? group = null;
        if (enrollment.CampEnrollmentGroupId is { } groupId)
        {
            group = await enrollments.GetGroupWithMembersAsync(groupId, organizationId, ct);
            bool isLeader = group is not null && group.LeaderEnrollmentId == enrollment.Id;
            bool otherActiveMembers = group is not null && group.Members.Any(m =>
                m.Id != enrollment.Id && m.Status != EnrollmentStatus.Cancelled);
            if (isLeader && otherActiveMembers)
                return Result.Fail(new Error(ErrorCodes.Validation,
                    "De groepsleider kan niet los geannuleerd worden zolang er actieve leden zijn. Annuleer de hele groep."));
        }

        enrollment.Status = EnrollmentStatus.Cancelled;

        // Een openstaande cash-betaling van de groep volgt het aantal actieve leden, zodat
        // de groep na een annulering niet te veel moet betalen.
        if (group is not null)
        {
            await SyncPendingCashAmountAsync(group.LeaderEnrollmentId, camp, group.Members, ct);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> UpdateEnrollmentAsync(
        Guid campId, Guid campEnrollmentId, Guid organizationId, UpdateCampEnrollmentRequest request,
        CancellationToken ct = default)
    {
        Camp? camp = await camps.GetByIdWithDetailsAsync(campId, organizationId, ct);
        if (camp is null) return Result.Fail(new Error(ErrorCodes.NotFound, "Kamp niet gevonden."));

        // GetByIdWithResponsesAsync filtert op organizationId; de CampId-check voorkomt
        // een cross-kamp wijziging binnen dezelfde organisatie.
        CampEnrollment? enrollment = await enrollments.GetByIdWithResponsesAsync(campEnrollmentId, organizationId, ct);
        if (enrollment is null || enrollment.CampId != campId)
            return Result.Fail(new Error(ErrorCodes.NotFound, "Inschrijving niet gevonden."));

        if (enrollment.Status == EnrollmentStatus.Cancelled)
            return Result.Fail(new Error(ErrorCodes.Conflict,
                "Een geannuleerde inschrijving kan niet aangepast worden."));

        string email = request.ParticipantEmail.Trim();
        if (await enrollments.IsDuplicateExceptAsync(campId, email, campEnrollmentId, ct))
            return Result.Fail(new Error(ErrorCodes.Conflict,
                $"{email} is al ingeschreven voor dit kamp."));

        CampEnrollmentForm? form = await forms.GetByCampIdReadOnlyAsync(campId, ct);
        List<CampFormField> fields = form?.Fields.ToList() ?? [];
        Error? formError = FormResponseValidator.Validate(
            fields.Select(f => (f.Id, f.IsRequired, f.Label)),
            request.Responses.Select(r => (r.CampFormFieldId, r.Value)));
        if (formError is not null)
            return Result.Fail(formError);

        Error? priceError = await ApplyPriceChangeAsync(camp, enrollment, request.CampPriceId, organizationId, ct);
        if (priceError is not null)
            return Result.Fail(priceError);

        enrollment.ParticipantName = request.ParticipantName.Trim();
        enrollment.ParticipantEmail = email;
        enrollment.ParticipantPhone = string.IsNullOrWhiteSpace(request.ParticipantPhone)
            ? null
            : request.ParticipantPhone.Trim();

        // Volledige set: per bestaand veld van het formulier bijwerken, toevoegen of wissen.
        // Antwoorden op velden die intussen uit het formulier verdwenen zijn blijven staan.
        foreach (CampFormField field in fields)
        {
            string? incoming = request.Responses
                .FirstOrDefault(r => r.CampFormFieldId == field.Id)?.Value;
            bool hasValue = !string.IsNullOrWhiteSpace(incoming);
            CampFormResponse? existing = enrollment.FormResponses
                .FirstOrDefault(r => r.CampFormFieldId == field.Id);

            if (existing is not null && !hasValue)
            {
                enrollments.RemoveFormResponse(existing);
            }
            else if (existing is not null)
            {
                existing.Value = incoming!;
            }
            else if (hasValue)
            {
                await enrollments.AddFormResponseAsync(new CampFormResponse
                {
                    CampEnrollmentId = enrollment.Id,
                    CampFormFieldId = field.Id,
                    Value = incoming!,
                }, ct);
            }
        }

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result<Guid>> AddGroupMemberAsync(
        Guid campId, Guid groupId, Guid organizationId, AddCampGroupMemberRequest request,
        CancellationToken ct = default)
    {
        Camp? camp = await camps.GetByIdWithDetailsAsync(campId, organizationId, ct);
        if (camp is null)
            return Result<Guid>.Fail(new Error(ErrorCodes.NotFound, "Kamp niet gevonden."));

        CampEnrollmentGroup? group = await enrollments.GetGroupWithMembersAsync(groupId, organizationId, ct);
        if (group is null || group.CampId != campId)
            return Result<Guid>.Fail(new Error(ErrorCodes.NotFound, "Groep niet gevonden."));

        CampEnrollment? leader = group.Members.FirstOrDefault(m =>
            m.Id == group.LeaderEnrollmentId && m.Status != EnrollmentStatus.Cancelled);
        if (leader is null)
            return Result<Guid>.Fail(new Error(ErrorCodes.Conflict,
                "Je kan geen lid toevoegen aan een groep zonder actieve leider."));

        // De groepsbetaling is prijs × leden en staat vast zodra ze betaald is of online loopt.
        Payment? payment = await payments.GetLatestTrackedByCampEnrollmentIdAsync(leader.Id, ct);
        if (payment is not null
            && (payment.Status == PaymentStatus.Paid
                || (payment.Status == PaymentStatus.Pending && payment.Method != PaymentMethod.Cash)))
            return Result<Guid>.Fail(new Error(ErrorCodes.Conflict,
                "Je kan geen lid toevoegen aan een groep waarvan de betaling al betaald is of online loopt."));

        CampEnrollmentForm? form = await forms.GetByCampIdReadOnlyAsync(campId, ct);
        // Altijd valideren, ook zonder formulier: dan is de veldenset leeg en wordt elk antwoord
        // met een onbekend veld-id (bv. uit een ander kamp) netjes geweigerd i.p.v. opgeslagen.
        Error? formError = FormResponseValidator.Validate(
            (form?.Fields ?? []).Select(f => (f.Id, f.IsRequired, f.Label)),
            request.Responses.Select(r => (r.CampFormFieldId, r.Value)));
        if (formError is not null)
            return Result<Guid>.Fail(formError);

        if (request.CampPriceId is { } priceId && camp.Prices.All(p => p.Id != priceId))
            return Result<Guid>.Fail(new Error(ErrorCodes.NotFound, "Prijsoptie niet gevonden."));

        // Een groep zonder betaling (bevestigd, gratis) kan geen lid met een betaald tarief krijgen.
        if (payment is null && leader.Status == EnrollmentStatus.Confirmed
            && CampPricing.UnitPrice(camp, new CampEnrollment { CampPriceId = request.CampPriceId }) > 0m)
            return Result<Guid>.Fail(new Error(ErrorCodes.Conflict,
                "Deze groep is bevestigd zonder betaling; een lid met een betaald tarief kan niet toegevoegd worden."));

        string email = request.ParticipantEmail.Trim();

        await unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            if (camp.MaxParticipants.HasValue
                && await enrollments.CountActiveByCampAsync(campId, ct) + 1 > camp.MaxParticipants.Value)
            {
                await unitOfWork.RollbackTransactionAsync(ct);
                return Result<Guid>.Fail(new Error(ErrorCodes.Conflict, "Dit kamp is volzet."));
            }

            if (await enrollments.IsDuplicateAsync(campId, email, ct))
            {
                await unitOfWork.RollbackTransactionAsync(ct);
                return Result<Guid>.Fail(new Error(ErrorCodes.Conflict,
                    $"{email} is al ingeschreven voor dit kamp."));
            }

            CampEnrollment member = new()
            {
                OrganizationId = organizationId,
                CampId = campId,
                ParticipantName = request.ParticipantName.Trim(),
                ParticipantEmail = email,
                ParticipantPhone = string.IsNullOrWhiteSpace(request.ParticipantPhone)
                    ? null
                    : request.ParticipantPhone.Trim(),
                Status = leader.Status, // status geërfd van de groep
                EnrolledAt = DateTime.UtcNow,
                CampEnrollmentGroupId = groupId,
                CampPriceId = request.CampPriceId,
            };
            await enrollments.AddAsync(member, ct);

            foreach (CampFormResponseValueDto r in request.Responses)
                await enrollments.AddFormResponseAsync(new CampFormResponse
                {
                    CampEnrollmentId = member.Id, CampFormFieldId = r.CampFormFieldId, Value = r.Value,
                }, ct);

            if (payment is not null && payment.Status == PaymentStatus.Pending && payment.Method == PaymentMethod.Cash)
            {
                // EF voegt het nieuwe lid via relationship fixup al toe aan group.Members; tel het
                // daarom expliciet apart zodat het niet dubbel meetelt.
                payment.Amount = CampPricing.Total(camp, group.Members.Where(m => m.Id != member.Id).Append(member));
            }

            await unitOfWork.SaveChangesAsync(ct);
            await unitOfWork.CommitTransactionAsync(ct);
            return Result<Guid>.Ok(member.Id);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }
    }

    public async Task<Result> CancelGroupAsync(
        Guid campId, Guid groupId, Guid organizationId, CancellationToken ct = default)
    {
        bool exists = await camps.ExistsAsync(campId, organizationId, ct);
        if (!exists) return Result.Fail(new Error(ErrorCodes.NotFound, "Kamp niet gevonden."));

        CampEnrollmentGroup? group = await enrollments.GetGroupWithMembersAsync(groupId, organizationId, ct);
        if (group is null || group.CampId != campId)
            return Result.Fail(new Error(ErrorCodes.NotFound, "Groep niet gevonden."));

        List<CampEnrollment> active = group.Members
            .Where(m => m.Status != EnrollmentStatus.Cancelled)
            .ToList();
        if (active.Count == 0)
            return Result.Fail(new Error(ErrorCodes.Validation, "Deze groep is al geannuleerd."));

        foreach (CampEnrollment member in active)
            member.Status = EnrollmentStatus.Cancelled;

        // Eén SaveChanges = één impliciete transactie: alles-of-niets.
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Ok();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Zet het bedrag van een openstaande cash-betaling van de groepsleider op
    /// <c>prijs × actieve leden</c>. Betaalde of online lopende betalingen blijven ongemoeid.
    /// </summary>
    private async Task SyncPendingCashAmountAsync(
        Guid payerEnrollmentId, Camp camp, IEnumerable<CampEnrollment> participants, CancellationToken ct)
    {
        Payment? payment = await payments.GetLatestTrackedByCampEnrollmentIdAsync(payerEnrollmentId, ct);
        if (payment is null || payment.Status != PaymentStatus.Pending || payment.Method != PaymentMethod.Cash)
            return;

        payment.Amount = CampPricing.Total(camp, participants);
    }

    /// <summary>Herberekent de openstaande cash-betalingen van alle actieve betalers van het kamp.</summary>
    private async Task SyncAllPendingCashAmountsAsync(Camp camp, Guid organizationId, CancellationToken ct)
    {
        List<CampEnrollment> all = await enrollments.GetByCampWithResponsesAsync(camp.Id, organizationId, ct);
        List<CampEnrollment> active = all.Where(e => e.Status != EnrollmentStatus.Cancelled).ToList();

        foreach (CampEnrollment solo in active.Where(e => e.CampEnrollmentGroupId is null))
            await SyncPendingCashAmountAsync(solo.Id, camp, [solo], ct);

        foreach (Guid groupId in active.Where(e => e.CampEnrollmentGroupId is not null)
                     .Select(e => e.CampEnrollmentGroupId!.Value).Distinct())
        {
            CampEnrollmentGroup? group = await enrollments.GetGroupWithMembersAsync(groupId, organizationId, ct);
            if (group is not null)
                await SyncPendingCashAmountAsync(group.LeaderEnrollmentId, camp, group.Members, ct);
        }
    }

    /// <summary>
    /// Wijzigt het tarief van een inschrijving en past een openstaande cash-betaling van de betaler
    /// (groepsleider of de inschrijving zelf) aan. Een betaalde of online lopende betaling staat vast.
    /// </summary>
    private async Task<Error?> ApplyPriceChangeAsync(
        Camp camp, CampEnrollment enrollment, Guid? newPriceId, Guid organizationId, CancellationToken ct)
    {
        if (newPriceId == enrollment.CampPriceId)
            return null;
        if (newPriceId is { } id && camp.Prices.All(p => p.Id != id))
            return new Error(ErrorCodes.NotFound, "Prijsoptie niet gevonden.");

        CampEnrollmentGroup? group = enrollment.CampEnrollmentGroupId is { } groupId
            ? await enrollments.GetGroupWithMembersAsync(groupId, organizationId, ct)
            : null;
        Guid payerId = group?.LeaderEnrollmentId ?? enrollment.Id;

        Payment? payment = await payments.GetLatestTrackedByCampEnrollmentIdAsync(payerId, ct);
        if (payment is not null
            && (payment.Status == PaymentStatus.Paid
                || (payment.Status == PaymentStatus.Pending && payment.Method != PaymentMethod.Cash)))
            return new Error(ErrorCodes.Conflict,
                "De betaling is al betaald of loopt online; het tarief kan niet meer gewijzigd worden.");

        // Een bevestigde inschrijving zonder betaling (gratis) kan geen betaald tarief krijgen: er zou
        // niets te betalen of te registreren zijn terwijl de deelnemer bevestigd blijft.
        CampEnrollment payer = group?.Members.FirstOrDefault(m => m.Id == payerId) ?? enrollment;
        Guid? previousPriceId = enrollment.CampPriceId;
        enrollment.CampPriceId = newPriceId;
        IEnumerable<CampEnrollment> participants = group?.Members ?? [enrollment];
        if (payment is null && payer.Status == EnrollmentStatus.Confirmed
            && CampPricing.Total(camp, participants) > 0m)
        {
            enrollment.CampPriceId = previousPriceId;
            return new Error(ErrorCodes.Conflict,
                "Deze inschrijving is bevestigd zonder betaling; een betaald tarief kan niet meer gekozen worden.");
        }

        if (payment is not null && payment.Status == PaymentStatus.Pending && payment.Method == PaymentMethod.Cash)
            payment.Amount = CampPricing.Total(camp, participants);
        return null;
    }

    private async Task<Error?> ValidateClubAndTrainersAsync(
        Guid organizationId, Guid clubId, List<CreateCampDayRequest> days, CancellationToken ct)
    {
        bool clubExists = await clubs.ExistsAsync(clubId, organizationId, ct);
        if (!clubExists) return new Error(ErrorCodes.NotFound, "Club niet gevonden");

        IEnumerable<Guid> trainerIds = days.SelectMany(d => d.Trainers.Select(t => t.TrainerId)).Distinct();
        foreach (Guid trainerId in trainerIds)
        {
            bool active = await users.IsActiveTrainerAsync(trainerId, organizationId, ct);
            if (!active) return new Error(ErrorCodes.NotFound, "Trainer niet gevonden");
        }
        return null;
    }

    private Camp BuildCamp(Guid organizationId, CreateCampRequest request)
    {
        Camp camp = new()
        {
            OrganizationId = organizationId,
            TennisClubId = request.TennisClubId,
            Level = request.Level.HasValue ? (LessonLevel)request.Level.Value : null,
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            StartDate = ParseDate(request.StartDate),
            EndDate = ParseDate(request.EndDate),
            RegistrationDeadline = DateTime.SpecifyKind(request.RegistrationDeadline, DateTimeKind.Utc),
            MaxParticipants = request.MaxParticipants,
            IsActive = true,
        };
        foreach (CampDay day in BuildDays(organizationId, request.Days))
            camp.Days.Add(day);
        int order = 0;
        foreach (SaveCampPriceRequest p in request.Prices ?? [])
            camp.Prices.Add(NewPrice(organizationId, p, order++));
        order = 0;
        foreach (SaveCampPackageRequest p in request.Packages ?? [])
            camp.Packages.Add(NewPackage(organizationId, p, order++));
        return camp;
    }

    private static List<CampDay> BuildDays(Guid organizationId, List<CreateCampDayRequest> dayRequests)
    {
        List<CampDay> result = new();
        foreach (CreateCampDayRequest d in dayRequests)
        {
            CampDay day = new()
            {
                OrganizationId = organizationId,
                Date = ParseDate(d.Date),
                StartTime = TimeOnly.ParseExact(d.StartTime, "HH:mm"),
                EndTime = TimeOnly.ParseExact(d.EndTime, "HH:mm"),
            };
            int partOrder = 0;
            foreach (CreateCampDayPartRequest part in d.Parts ?? [])
            {
                day.Parts.Add(new CampDayPart
                {
                    OrganizationId = organizationId,
                    Order = partOrder++,
                    Name = part.Name.Trim(),
                    StartTime = TimeOnly.ParseExact(part.StartTime, "HH:mm"),
                    EndTime = TimeOnly.ParseExact(part.EndTime, "HH:mm"),
                });
            }
            // Met dagdelen zijn de kampuren van de dag de omhullende van de dagdelen.
            if (day.Parts.Count > 0)
            {
                day.StartTime = day.Parts.Min(x => x.StartTime);
                day.EndTime = day.Parts.Max(x => x.EndTime);
            }
            foreach (CreateCampDayTrainerRequest t in d.Trainers)
            {
                day.TrainerAssignments.Add(new CampDayTrainer
                {
                    OrganizationId = organizationId,
                    TrainerId = t.TrainerId,
                    StartTime = TimeOnly.ParseExact(t.StartTime, "HH:mm"),
                    EndTime = TimeOnly.ParseExact(t.EndTime, "HH:mm"),
                });
            }
            result.Add(day);
        }
        return result;
    }

    private async Task<List<CampDayDto>> BuildDayDtosAsync(Camp camp, Guid organizationId, CancellationToken ct)
    {
        // Verzamel trainernamen in een lookup om N+1 te vermijden.
        List<Guid> trainerIds = camp.Days.SelectMany(d => d.TrainerAssignments.Select(t => t.TrainerId)).Distinct().ToList();
        Dictionary<Guid, string> names = new();
        foreach (Guid id in trainerIds)
        {
            (string FullName, string Email)? info = await users.GetUserInfoByIdAsync(id, ct);
            names[id] = info.HasValue ? info.Value.FullName : string.Empty;
        }

        return camp.Days.OrderBy(d => d.Date).Select(d => new CampDayDto(
            d.Id, d.Date.ToString(DateFormat), d.StartTime.ToString("HH\\:mm"), d.EndTime.ToString("HH\\:mm"),
            d.TrainerAssignments.Select(t => new CampDayTrainerDto(
                t.TrainerId, names.GetValueOrDefault(t.TrainerId, string.Empty),
                t.StartTime.ToString("HH\\:mm"), t.EndTime.ToString("HH\\:mm"))).ToList(),
            MapParts(d))).ToList();
    }

    private static List<CampPriceDto> MapPrices(Camp camp)
        => camp.Prices.OrderBy(p => p.SortOrder)
            .Select(p => new CampPriceDto(p.Id, p.Label, p.Description, p.Discount)).ToList();

    internal static List<CampDayPartDto> MapParts(CampDay day)
        => day.Parts.OrderBy(x => x.Order)
            .Select(x => new CampDayPartDto(
                x.Order, x.Name, x.StartTime.ToString("HH\\:mm"), x.EndTime.ToString("HH\\:mm"))).ToList();

    internal static List<CampPackageDto> MapPackages(Camp camp, Func<CampPackage, bool>? isFull = null)
        => camp.Packages.OrderBy(p => p.SortOrder)
            .Select(p => new CampPackageDto(
                p.Id, p.Name, p.Description, p.Price,
                p.Slots.OrderBy(s => s.Date).ThenBy(s => s.PartOrder)
                    .Select(s => new CampPackageSlotDto(s.Date.ToString(DateFormat), s.PartOrder)).ToList(),
                isFull?.Invoke(p) ?? false)).ToList();

    private static CampPackage NewPackage(Guid organizationId, SaveCampPackageRequest p, int order)
    {
        CampPackage package = new()
        {
            OrganizationId = organizationId,
            Name = p.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(p.Description) ? null : p.Description.Trim(),
            Price = p.Price,
            SortOrder = order,
        };
        foreach (CampPackageSlotDto slot in p.Slots)
            package.Slots.Add(NewSlot(organizationId, slot));
        return package;
    }

    private static CampPackageSlot NewSlot(Guid organizationId, CampPackageSlotDto slot) => new()
    {
        OrganizationId = organizationId,
        Date = ParseDate(slot.Date),
        PartOrder = slot.PartOrder,
    };

    /// <summary>Elk slot van elk pakket moet wijzen naar een dagdeel (datum + positie) van de aangeleverde dagen.</summary>
    private static Error? ValidatePackageSlots(List<CreateCampDayRequest> days, List<SaveCampPackageRequest>? packages)
    {
        if (packages is null || packages.Count == 0) return null;

        HashSet<(DateOnly Date, int Order)> existing = days
            .SelectMany(d => Enumerable.Range(0, d.Parts?.Count ?? 0).Select(i => (ParseDate(d.Date), i)))
            .ToHashSet();
        bool missing = packages.SelectMany(p => p.Slots).Any(s => !existing.Contains((ParseDate(s.Date), s.PartOrder)));
        return missing
            ? new Error(ErrorCodes.Validation, "Een pakket verwijst naar een dagdeel dat niet meer bestaat.")
            : null;
    }

    /// <summary>
    /// Brengt de pakketten van het kamp in lijn met de aangeleverde lijst: bestaande (op Id) worden bijgewerkt
    /// (dagdelen via diff), nieuwe toegevoegd, ontbrekende verwijderd. Een pakket dat een inschrijving gekozen
    /// heeft kan niet verwijderd worden.
    /// </summary>
    private async Task<Error?> SyncPackagesAsync(
        Camp camp, Guid organizationId, List<SaveCampPackageRequest> incoming, CancellationToken ct)
    {
        List<Guid> incomingIds = incoming.Where(p => p.Id.HasValue).Select(p => p.Id!.Value).ToList();
        if (incomingIds.Any(id => camp.Packages.All(p => p.Id != id)))
            return new Error(ErrorCodes.NotFound, "Pakket niet gevonden.");

        List<CampPackage> removed = camp.Packages.Where(p => !incomingIds.Contains(p.Id)).ToList();
        if (await enrollments.AnyUsingPackagesAsync(removed.Select(p => p.Id), ct))
            return new Error(ErrorCodes.Conflict,
                "Een pakket dat bij een inschrijving gekozen is kan niet verwijderd worden.");

        if (removed.Count > 0)
        {
            camps.RemovePackages(removed);
            foreach (CampPackage p in removed) camp.Packages.Remove(p);
        }

        int order = 0;
        foreach (SaveCampPackageRequest dto in incoming)
        {
            CampPackage? existing = dto.Id.HasValue ? camp.Packages.FirstOrDefault(p => p.Id == dto.Id.Value) : null;
            if (existing is null)
            {
                camp.Packages.Add(NewPackage(organizationId, dto, order));
            }
            else
            {
                existing.Name = dto.Name.Trim();
                existing.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
                existing.Price = dto.Price;
                existing.SortOrder = order;

                HashSet<(DateOnly, int)> wanted = dto.Slots.Select(s => (ParseDate(s.Date), s.PartOrder)).ToHashSet();
                List<CampPackageSlot> dropped = existing.Slots.Where(s => !wanted.Contains((s.Date, s.PartOrder))).ToList();
                if (dropped.Count > 0)
                {
                    camps.RemovePackageSlots(dropped);
                    foreach (CampPackageSlot s in dropped) existing.Slots.Remove(s);
                }
                HashSet<(DateOnly, int)> kept = existing.Slots.Select(s => (s.Date, s.PartOrder)).ToHashSet();
                foreach (CampPackageSlotDto slot in dto.Slots.Where(s => !kept.Contains((ParseDate(s.Date), s.PartOrder))))
                    existing.Slots.Add(NewSlot(organizationId, slot));
            }
            order++;
        }
        return null;
    }

    private static CampPrice NewPrice(Guid organizationId, SaveCampPriceRequest p, int order) => new()
    {
        OrganizationId = organizationId,
        Label = p.Label.Trim(),
        Description = string.IsNullOrWhiteSpace(p.Description) ? null : p.Description.Trim(),
        Discount = p.Discount,
        SortOrder = order,
    };

    /// <summary>
    /// Brengt de prijsopties van het kamp in lijn met de aangeleverde lijst: bestaande (op Id) worden
    /// bijgewerkt, nieuwe toegevoegd, ontbrekende verwijderd. Een tarief dat een inschrijving gekozen
    /// heeft kan niet verwijderd worden.
    /// </summary>
    private async Task<Error?> SyncPricesAsync(
        Camp camp, Guid organizationId, List<SaveCampPriceRequest> incoming, CancellationToken ct)
    {
        List<Guid> incomingIds = incoming.Where(p => p.Id.HasValue).Select(p => p.Id!.Value).ToList();
        if (incomingIds.Any(id => camp.Prices.All(p => p.Id != id)))
            return new Error(ErrorCodes.NotFound, "Prijsoptie niet gevonden.");

        List<CampPrice> removed = camp.Prices.Where(p => !incomingIds.Contains(p.Id)).ToList();
        if (await enrollments.AnyUsingPricesAsync(removed.Select(p => p.Id), ct))
            return new Error(ErrorCodes.Conflict,
                "Een prijsoptie die bij een inschrijving gekozen is kan niet verwijderd worden.");

        camps.RemovePrices(removed);
        foreach (CampPrice p in removed) camp.Prices.Remove(p);

        int order = 0;
        foreach (SaveCampPriceRequest dto in incoming)
        {
            CampPrice? existing = dto.Id.HasValue ? camp.Prices.FirstOrDefault(p => p.Id == dto.Id.Value) : null;
            if (existing is null)
            {
                camp.Prices.Add(NewPrice(organizationId, dto, order));
            }
            else
            {
                existing.Label = dto.Label.Trim();
                existing.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
                existing.Discount = dto.Discount;
                existing.SortOrder = order;
            }
            order++;
        }
        return null;
    }

    private static DateOnly ParseDate(string d) => DateOnly.ParseExact(d, DateFormat, CultureInfo.InvariantCulture);

    private static List<string>? DeserializeOptions(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try { return JsonSerializer.Deserialize<List<string>>(json); }
        catch (JsonException) { return null; }
    }
}
