using CoachOS.Application.Abstractions;
using System.Data;
using System.Text.Json;
using CoachOS.Application.Camps.DTOs;
using CoachOS.Application.Common;
using CoachOS.Application.MollieConnect;
using CoachOS.Application.MollieConnect.DTOs;
using CoachOS.Application.Payments;
using CoachOS.Application.Payments.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using Microsoft.Extensions.Logging;

namespace CoachOS.Application.Camps;

public class CampEnrollmentService(
    ICampRepository camps,
    ICampEnrollmentRepository enrollments,
    ICampEnrollmentFormRepository forms,
    IPaymentService paymentService,
    IPaymentRepository payments,
    IUnitOfWork unitOfWork,
    IMollieConnectService mollieConnect,
    IEmailService emailService,
    ILogger<CampEnrollmentService> logger) : ICampEnrollmentService
{
    private const string DateFormat = "yyyy-MM-dd";

    public async Task<Result<PublicCampDto>> GetPublicCampAsync(Guid campId, CancellationToken ct = default)
    {
        Camp? camp = await camps.GetByIdPublicAsync(campId, ct);
        if (camp is null) return Result<PublicCampDto>.Fail(new Error(ErrorCodes.NotFound, "Kamp niet gevonden."));

        int participants = await enrollments.CountActiveByCampAsync(campId, ct);
        List<Guid?> activePackageIds = camp.Packages.Count > 0 && camp.MaxParticipants.HasValue
            ? (await enrollments.GetActiveByCampAsync(campId, ct)).Select(e => e.CampPackageId).ToList()
            : [];
        List<CampDayDto> days = camp.Days.OrderBy(d => d.Date).Select(d => new CampDayDto(
            d.Id, d.Date.ToString(DateFormat), d.StartTime.ToString("HH\\:mm"), d.EndTime.ToString("HH\\:mm"),
            new List<CampDayTrainerDto>(), CampService.MapParts(d))).ToList();

        return Result<PublicCampDto>.Ok(new PublicCampDto(
            camp.Id, camp.Name, camp.Description, camp.Level.HasValue ? (int)camp.Level.Value : null,
            camp.Price, camp.StartDate.ToString(DateFormat), camp.EndDate.ToString(DateFormat),
            camp.RegistrationDeadline, camp.TennisClub?.Name ?? string.Empty,
            camp.MaxParticipants, participants, days,
            camp.Prices.OrderBy(p => p.SortOrder)
                .Select(p => new CampPriceDto(p.Id, p.Label, p.Description, p.Discount)).ToList(),
            CampService.MapPackages(camp, p => CampCapacity.IsPackageFull(camp, p, activePackageIds))));
    }

    public async Task<Result<CampEnrollmentFormDto?>> GetPublicFormAsync(Guid campId, CancellationToken ct = default)
    {
        CampEnrollmentForm? form = await forms.GetByCampIdReadOnlyAsync(campId, ct);
        if (form is null) return Result<CampEnrollmentFormDto?>.Ok(null);
        return Result<CampEnrollmentFormDto?>.Ok(new CampEnrollmentFormDto
        {
            Id = form.Id,
            CampId = form.CampId,
            Fields = form.Fields.OrderBy(f => f.Order).Select(f => new CampFormFieldDto
            {
                Id = f.Id, Label = f.Label, Type = (int)f.Type, IsRequired = f.IsRequired, Order = f.Order,
                Options = DeserializeOptions(f.Options),
            }).ToList(),
        });
    }

    public async Task<Result<SubmitCampEnrollmentResultDto>> SubmitAsync(
        Guid campId, SubmitCampEnrollmentRequest request, CancellationToken ct = default)
    {
        Camp? camp = await camps.GetByIdPublicAsync(campId, ct);
        if (camp is null)
            return Result<SubmitCampEnrollmentResultDto>.Fail(new Error(ErrorCodes.NotFound, "Kamp niet gevonden."));

        if (DateTime.UtcNow > camp.RegistrationDeadline)
            return Result<SubmitCampEnrollmentResultDto>.Fail(new Error(ErrorCodes.Validation, "De inschrijvingsdeadline is verstreken."));

        CampEnrollmentForm? form = await forms.GetByCampIdReadOnlyAsync(campId, ct);
        // Altijd valideren, ook zonder formulier: dan is de veldenset leeg en wordt elk antwoord
        // met een onbekend veld-id (bv. uit een ander kamp) netjes geweigerd i.p.v. opgeslagen.
        Error? formError = FormResponseValidator.Validate(
            (form?.Fields ?? []).Select(f => (f.Id, f.IsRequired, f.Label)),
            request.Responses.Select(r => (r.CampFormFieldId, r.Value)));
        if (formError is not null)
            return Result<SubmitCampEnrollmentResultDto>.Fail(formError);

        int groupSize = request.EnrollmentType == "group" && request.GroupMembers is not null
            ? request.GroupMembers.Count + 1
            : 1;

        // In-request duplicate check (cheap, no race concern): leader + members must have unique emails.
        if (request.EnrollmentType == "group" && request.GroupMembers is { Count: > 0 })
        {
            List<string> emails = new() { request.ParticipantEmail.Trim().ToLowerInvariant() };
            emails.AddRange(request.GroupMembers.Select(m => m.ParticipantEmail.Trim().ToLowerInvariant()));
            if (emails.Count != emails.Distinct().Count())
                return Result<SubmitCampEnrollmentResultDto>.Fail(
                    new Error(ErrorCodes.Conflict, "Een e-mailadres komt meerdere keren voor in deze inschrijving."));
        }

        // Heeft het kamp pakketten, dan kiest elke deelnemer er één; anders worden pakket-ids genegeerd.
        bool hasPackages = camp.Packages.Count > 0;
        List<CampGroupMemberDto> members = request.EnrollmentType == "group" && request.GroupMembers is { Count: > 0 }
            ? request.GroupMembers
            : [];
        Guid? leaderPackageId = hasPackages ? request.CampPackageId : null;
        List<Guid?> memberPackageIds = members.Select(m => hasPackages ? m.CampPackageId : null).ToList();
        if (hasPackages
            && memberPackageIds.Prepend(leaderPackageId)
                .Any(id => id is null || camp.Packages.All(pkg => pkg.Id != id.Value)))
            return Result<SubmitCampEnrollmentResultDto>.Fail(
                new Error(ErrorCodes.Validation, "Kies een pakket voor elke deelnemer."));

        // Betaald = de som van de gekozen pakketten (of hoofdprijzen) is meer dan 0.
        decimal total = CampPricing.Total(camp,
            memberPackageIds.Prepend(leaderPackageId).Select(id => new CampEnrollment { CampPackageId = id }));
        bool isPaid = total > 0m;
        EnrollmentStatus initialStatus = isPaid ? EnrollmentStatus.PendingPayment : EnrollmentStatus.Confirmed;

        CampEnrollment enrollment;
        try
        {
            await unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable, ct);

            if (camp.MaxParticipants.HasValue)
            {
                bool full;
                if (hasPackages)
                {
                    List<CampEnrollment> active = await enrollments.GetActiveByCampAsync(campId, ct);
                    full = CampCapacity.IsFull(
                        camp, active.Select(e => e.CampPackageId), memberPackageIds.Prepend(leaderPackageId));
                }
                else
                {
                    full = await enrollments.CountActiveByCampAsync(campId, ct) + groupSize > camp.MaxParticipants.Value;
                }
                if (full)
                {
                    await unitOfWork.RollbackTransactionAsync(ct);
                    return Result<SubmitCampEnrollmentResultDto>.Fail(new Error(ErrorCodes.Conflict, "Dit kamp is volzet."));
                }
            }

            bool duplicate = await enrollments.IsDuplicateAsync(campId, request.ParticipantEmail, ct);
            if (duplicate)
            {
                await unitOfWork.RollbackTransactionAsync(ct);
                return Result<SubmitCampEnrollmentResultDto>.Fail(new Error(ErrorCodes.Conflict, "Je bent al ingeschreven voor dit kamp."));
            }

            if (request.EnrollmentType == "group" && request.GroupMembers is { Count: > 0 })
            {
                foreach (CampGroupMemberDto member in request.GroupMembers)
                {
                    bool memberDuplicate = await enrollments.IsDuplicateAsync(campId, member.ParticipantEmail, ct);
                    if (memberDuplicate)
                    {
                        await unitOfWork.RollbackTransactionAsync(ct);
                        return Result<SubmitCampEnrollmentResultDto>.Fail(new Error(ErrorCodes.Conflict, "Een van de groepsleden is al ingeschreven voor dit kamp."));
                    }
                }
            }

            enrollment = new CampEnrollment
            {
                OrganizationId = camp.OrganizationId,
                CampId = camp.Id,
                ParticipantName = request.ParticipantName,
                ParticipantEmail = request.ParticipantEmail,
                ParticipantPhone = request.ParticipantPhone,
                Status = initialStatus,
                EnrolledAt = DateTime.UtcNow,
                CampPackageId = leaderPackageId,
            };
            await enrollments.AddAsync(enrollment, ct);

            foreach (CampFormResponseValueDto r in request.Responses)
                await enrollments.AddFormResponseAsync(new CampFormResponse
                {
                    CampEnrollmentId = enrollment.Id, CampFormFieldId = r.CampFormFieldId, Value = r.Value,
                }, ct);

            await unitOfWork.SaveChangesAsync(ct);

            if (request.EnrollmentType == "group" && request.GroupMembers is { Count: > 0 })
            {
                int existing = await enrollments.CountActiveByCampGroupsAsync(campId, camp.OrganizationId, ct);
                CampEnrollmentGroup group = new()
                {
                    OrganizationId = camp.OrganizationId,
                    CampId = camp.Id,
                    Name = $"Groep {BuildGroupName(existing)}",
                    LeaderEnrollmentId = enrollment.Id,
                };
                await enrollments.AddGroupAsync(group, ct);
                await unitOfWork.SaveChangesAsync(ct);

                enrollment.CampEnrollmentGroupId = group.Id;

                for (int i = 0; i < members.Count; i++)
                {
                    CampGroupMemberDto member = members[i];
                    CampEnrollment memberEnrollment = new()
                    {
                        OrganizationId = camp.OrganizationId,
                        CampId = camp.Id,
                        ParticipantName = member.ParticipantName,
                        ParticipantEmail = member.ParticipantEmail,
                        ParticipantPhone = member.ParticipantPhone,
                        Status = initialStatus,
                        EnrolledAt = DateTime.UtcNow,
                        CampEnrollmentGroupId = group.Id,
                        CampPackageId = memberPackageIds[i],
                    };
                    await enrollments.AddAsync(memberEnrollment, ct);

                    if (member.Responses is { Count: > 0 })
                        foreach (CampFormResponseValueDto r in member.Responses)
                            await enrollments.AddFormResponseAsync(new CampFormResponse
                            {
                                CampEnrollmentId = memberEnrollment.Id, CampFormFieldId = r.CampFormFieldId, Value = r.Value,
                            }, ct);
                }
                await unitOfWork.SaveChangesAsync(ct);
            }

            await unitOfWork.CommitTransactionAsync(ct);
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackTransactionAsync(ct);
            logger.LogError(ex, "Kampinschrijving mislukt voor kamp {CampId}", campId);
            return Result<SubmitCampEnrollmentResultDto>.Fail(new Error(ErrorCodes.Unexpected, "Inschrijving mislukt. Probeer het opnieuw."));
        }

        // Mails na commit. Betaalde kampen maken hier GEEN payment aan: de deelnemer
        // kiest zelf cash of online op de betaalpagina (ChoosePaymentAsync). Gratis
        // kampen zijn direct bevestigd en krijgen meteen een bevestigingsmail.
        if (!isPaid)
        {
            await SafeSendAsync(() => emailService.SendCampEnrollmentConfirmedAsync(
                request.ParticipantEmail, request.ParticipantName, camp.Name,
                camp.StartDate, camp.EndDate, ct), enrollment.Id);
        }

        return Result<SubmitCampEnrollmentResultDto>.Ok(
            new SubmitCampEnrollmentResultDto(enrollment.Id, RequiresPayment: isPaid));
    }

    public async Task<Result<Guid>> CreateManualAsync(
        Guid campId, Guid organizationId, CreateManualCampEnrollmentRequest request,
        CancellationToken ct = default)
    {
        // GetByIdPublicAsync is niet tenant-gefilterd: controleer de organisatie expliciet.
        Camp? camp = await camps.GetByIdPublicAsync(campId, ct);
        if (camp is null || camp.OrganizationId != organizationId)
            return Result<Guid>.Fail(new Error(ErrorCodes.NotFound, "Kamp niet gevonden."));

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
        if (request.CampPackageId is { } packageId && camp.Packages.All(p => p.Id != packageId))
            return Result<Guid>.Fail(new Error(ErrorCodes.NotFound, "Pakket niet gevonden."));

        string email = request.ParticipantEmail.Trim();
        // Betalend = het gekozen pakket (of de hoofdprijs) min de gekozen korting is meer dan 0.
        bool isPaidCamp = CampPricing.UnitPrice(camp,
            new CampEnrollment { CampPriceId = request.CampPriceId, CampPackageId = request.CampPackageId }) > 0m;
        CampEnrollment enrollment;

        await unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            if (await CampCapacity.IsFullAsync(enrollments, camp, [request.CampPackageId], ct))
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

            enrollment = new CampEnrollment
            {
                OrganizationId = organizationId,
                CampId = campId,
                ParticipantName = request.ParticipantName.Trim(),
                ParticipantEmail = email,
                ParticipantPhone = string.IsNullOrWhiteSpace(request.ParticipantPhone)
                    ? null
                    : request.ParticipantPhone.Trim(),
                // Betaald kamp: pas bevestigd zodra de cash als betaald is gemarkeerd (zelfde regel
                // als de publieke flow). Gratis kamp: meteen bevestigd.
                Status = isPaidCamp ? EnrollmentStatus.PendingPayment : EnrollmentStatus.Confirmed,
                EnrolledAt = DateTime.UtcNow,
                CampPriceId = request.CampPriceId,
                CampPackageId = request.CampPackageId,
            };
            await enrollments.AddAsync(enrollment, ct);

            foreach (CampFormResponseValueDto r in request.Responses.Where(r => !string.IsNullOrWhiteSpace(r.Value)))
                await enrollments.AddFormResponseAsync(new CampFormResponse
                {
                    CampEnrollmentId = enrollment.Id, CampFormFieldId = r.CampFormFieldId, Value = r.Value,
                }, ct);

            await unitOfWork.SaveChangesAsync(ct);

            if (isPaidCamp)
            {
                // Openstaande cash-betaling (prijs × 1), zodat de beheerder ze kan afrekenen.
                Result recorded = await paymentService.RecordCampCashPaymentAsync(enrollment.Id, organizationId, ct);
                if (!recorded.IsSuccess)
                {
                    await unitOfWork.RollbackTransactionAsync(ct);
                    return Result<Guid>.Fail(recorded.Errors);
                }
            }

            await unitOfWork.CommitTransactionAsync(ct);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }

        // Mails na commit (zelfde regel als de publieke flow): gratis meteen, betaald via "betaald markeren".
        if (!isPaidCamp)
        {
            await SafeSendAsync(() => emailService.SendCampEnrollmentConfirmedAsync(
                enrollment.ParticipantEmail, enrollment.ParticipantName, camp.Name,
                camp.StartDate, camp.EndDate, ct), enrollment.Id);
        }
        else if (request.Paid)
        {
            Result marked = await paymentService.MarkCampCashPaidAsync(campId, enrollment.Id, organizationId, ct);
            if (!marked.IsSuccess)
                logger.LogWarning(
                    "Manuele kampinschrijving {Id} aangemaakt maar niet als betaald gemarkeerd: {Error}",
                    enrollment.Id, marked.Errors.FirstOrDefault()?.Message);
        }

        return Result<Guid>.Ok(enrollment.Id);
    }

    public async Task<Result<CampPaymentOptionsDto>> GetPaymentOptionsAsync(
        Guid campId, Guid? campEnrollmentId = null, CancellationToken ct = default)
    {
        Camp? camp = await camps.GetByIdPublicAsync(campId, ct);
        if (camp is null)
            return Result<CampPaymentOptionsDto>.Fail(new Error(ErrorCodes.NotFound, "Kamp niet gevonden."));

        Result<MollieConnectionStatusDto> statusResult = await mollieConnect.GetStatusAsync(camp.OrganizationId, ct);
        bool onlineAvailable = statusResult.Value?.Connected ?? false;
        if (!statusResult.IsSuccess)
        {
            logger.LogWarning(
                "Kon Mollie-status niet ophalen voor org {OrgId}; online betaling wordt verborgen.",
                camp.OrganizationId);
            onlineAvailable = false;
        }

        decimal price = camp.Price;
        if (campEnrollmentId is { } enrollmentId)
        {
            CampEnrollment? enrollment = await enrollments.GetByIdWithGroupAsync(enrollmentId, ct);
            if (enrollment is null || enrollment.CampId != campId)
                return Result<CampPaymentOptionsDto>.Fail(new Error(ErrorCodes.NotFound, "Inschrijving niet gevonden."));

            // De betaler (solo of groepsleider) betaalt de som van alle niet-geannuleerde deelnemers.
            IEnumerable<CampEnrollment> participants =
                enrollment.CampEnrollmentGroupId.HasValue && enrollment.Group is not null
                    ? enrollment.Group.Members
                    : [enrollment];
            price = CampPricing.Total(camp, participants);
        }

        return Result<CampPaymentOptionsDto>.Ok(new CampPaymentOptionsDto(price, onlineAvailable));
    }

    public async Task<Result<ChooseCampPaymentResultDto>> ChoosePaymentAsync(
        Guid campEnrollmentId, int method, CancellationToken ct = default)
    {
        CampEnrollment? enrollment = await enrollments.GetByIdWithGroupAsync(campEnrollmentId, ct);
        if (enrollment is null)
            return Result<ChooseCampPaymentResultDto>.Fail(new Error(ErrorCodes.NotFound, "Inschrijving niet gevonden."));

        if (enrollment.Status == EnrollmentStatus.Confirmed)
            return Result<ChooseCampPaymentResultDto>.Fail(new Error(ErrorCodes.Conflict, "Al bevestigd."));

        // Voorkom dubbele betalingen: als er al een lopende (Pending) of geslaagde
        // (Paid) betaling bestaat, weiger een nieuwe keuze. Failed mag opnieuw.
        Payment? existing = await payments.GetLatestByCampEnrollmentIdAsync(campEnrollmentId, ct);
        if (existing is not null && existing.Status is PaymentStatus.Pending or PaymentStatus.Paid)
            return Result<ChooseCampPaymentResultDto>.Fail(new Error(ErrorCodes.Conflict, "Er loopt al een betaling."));

        // Expliciete guard: voorkom dat een onbekende int stilletjes als cash wordt
        // afgehandeld door de (PaymentMethod)-cast. Alleen gedefinieerde waarden mogen door.
        if (!Enum.IsDefined(typeof(PaymentMethod), method))
            return Result<ChooseCampPaymentResultDto>.Fail(new Error(ErrorCodes.Validation, "Ongeldige betaalmethode."));

        PaymentMethod chosen = (PaymentMethod)method;
        if (chosen == PaymentMethod.Online)
        {
            Result<CreatePaymentResultDto> paymentResult = await paymentService.CreatePaymentForCampEnrollmentAsync(
                campEnrollmentId, enrollment.OrganizationId, ct);
            if (!paymentResult.IsSuccess)
                return Result<ChooseCampPaymentResultDto>.Fail(paymentResult.Errors);

            return Result<ChooseCampPaymentResultDto>.Ok(
                new ChooseCampPaymentResultDto(paymentResult.Value!.CheckoutUrl));
        }

        if (chosen == PaymentMethod.Cash)
        {
            Result cashResult = await paymentService.RecordCampCashPaymentAsync(
                campEnrollmentId, enrollment.OrganizationId, ct);
            if (!cashResult.IsSuccess)
                return Result<ChooseCampPaymentResultDto>.Fail(cashResult.Errors);

            return Result<ChooseCampPaymentResultDto>.Ok(new ChooseCampPaymentResultDto(null));
        }

        // Gedefinieerd maar niet ondersteund als kamp-betaalkeuze.
        return Result<ChooseCampPaymentResultDto>.Fail(new Error(ErrorCodes.Validation, "Ongeldige betaalmethode."));
    }

    private async Task SafeSendAsync(Func<Task> send, Guid enrollmentId)
    {
        try { await send(); }
        catch (Exception ex) { logger.LogError(ex, "E-mail mislukt voor kampinschrijving {Id}", enrollmentId); }
    }

    private static string BuildGroupName(int index)
    {
        string name = string.Empty;
        int n = index;
        while (true)
        {
            name = (char)('A' + n % 26) + name;
            n = n / 26 - 1;
            if (n < 0) break;
        }
        return name;
    }

    private List<string>? DeserializeOptions(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try { return JsonSerializer.Deserialize<List<string>>(json); }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Ongeldige JSON in kamp-formulierveld opties: {Json}", json);
            return null;
        }
    }
}
