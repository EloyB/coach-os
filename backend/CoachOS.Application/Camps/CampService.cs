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
            camp.MaxParticipants, participants, camp.IsActive, days));
    }

    public async Task<Result<Guid>> CreateAsync(Guid organizationId, CreateCampRequest request, CancellationToken ct = default)
    {
        Error? validation = await ValidateClubAndTrainersAsync(organizationId, request.TennisClubId, request.Days, ct);
        if (validation is not null) return Result<Guid>.Fail(validation);

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

        camp.Name = request.Name;
        camp.Description = request.Description;
        camp.TennisClubId = request.TennisClubId;
        camp.Level = request.Level.HasValue ? (LessonLevel)request.Level.Value : null;
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

        Dictionary<Guid, (PaymentMethod? Method, PaymentStatus Status)> paymentInfo =
            await payments.GetLatestMethodAndStatusByCampEnrollmentIdsAsync(rows.Select(e => e.Id), ct);

        List<CampEnrollmentDto> dtos = rows.Select(e =>
        {
            (PaymentMethod? Method, PaymentStatus Status)? info =
                paymentInfo.TryGetValue(e.Id, out (PaymentMethod? Method, PaymentStatus Status) p) ? p : null;
            return new CampEnrollmentDto(
                e.Id, e.ParticipantName, e.ParticipantEmail, e.ParticipantPhone,
                e.Status.ToString(), e.EnrolledAt, e.Group?.Name,
                e.CampEnrollmentGroupId, e.Group?.LeaderEnrollmentId == e.Id,
                e.FormResponses.Select(r => new CampEnrollmentResponseItemDto(
                    r.CampFormFieldId, r.CampFormField?.Label ?? string.Empty, r.Value)).ToList(),
                info?.Method?.ToString(), info?.Status.ToString());
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
        bool exists = await camps.ExistsAsync(campId, organizationId, ct);
        if (!exists) return Result.Fail(new Error(ErrorCodes.NotFound, "Kamp niet gevonden."));

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
            int remaining = group.Members.Count(m => m.Id != enrollment.Id && m.Status != EnrollmentStatus.Cancelled);
            await SyncPendingCashAmountAsync(group.LeaderEnrollmentId, campId, organizationId, remaining, ct);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> UpdateEnrollmentAsync(
        Guid campId, Guid campEnrollmentId, Guid organizationId, UpdateCampEnrollmentRequest request,
        CancellationToken ct = default)
    {
        bool exists = await camps.ExistsAsync(campId, organizationId, ct);
        if (!exists) return Result.Fail(new Error(ErrorCodes.NotFound, "Kamp niet gevonden."));

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
            fields.Select(f => (f.Id, f.IsRequired, f.Label, ChoiceOptions(f.Type, f.Options))),
            request.Responses.Select(r => (r.CampFormFieldId, r.Value)));
        if (formError is not null)
            return Result.Fail(formError);

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
            (form?.Fields ?? []).Select(f => (f.Id, f.IsRequired, f.Label, ChoiceOptions(f.Type, f.Options))),
            request.Responses.Select(r => (r.CampFormFieldId, r.Value)));
        if (formError is not null)
            return Result<Guid>.Fail(formError);

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
                int active = group.Members.Count(m => m.Id != member.Id && m.Status != EnrollmentStatus.Cancelled) + 1;
                payment.Amount = camp.Price * active;
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
        Guid leaderEnrollmentId, Guid campId, Guid organizationId, int activeCount, CancellationToken ct)
    {
        Payment? payment = await payments.GetLatestTrackedByCampEnrollmentIdAsync(leaderEnrollmentId, ct);
        if (payment is null || payment.Status != PaymentStatus.Pending || payment.Method != PaymentMethod.Cash)
            return;

        Camp? camp = await camps.GetByIdWithDetailsAsync(campId, organizationId, ct);
        if (camp is null)
            return;

        payment.Amount = camp.Price * Math.Max(activeCount, 1);
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
                t.StartTime.ToString("HH\\:mm"), t.EndTime.ToString("HH\\:mm"))).ToList())).ToList();
    }

    private static DateOnly ParseDate(string d) => DateOnly.ParseExact(d, DateFormat, CultureInfo.InvariantCulture);

    private static List<string>? DeserializeOptions(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try { return JsonSerializer.Deserialize<List<string>>(json); }
        catch (JsonException) { return null; }
    }

    /// <summary>
    /// The set of values a choice field accepts (its configured options), used to reject
    /// submitted responses that aren't one of the offered options. Null for non-choice fields.
    /// </summary>
    private static IReadOnlyList<string>? ChoiceOptions(FormFieldType type, string? optionsJson)
        => type is FormFieldType.MultipleChoice or FormFieldType.AgeCategory
            ? DeserializeOptions(optionsJson)
            : null;
}
