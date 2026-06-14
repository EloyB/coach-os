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
        List<CampDayDto> days = camp.Days.OrderBy(d => d.Date).Select(d => new CampDayDto(
            d.Id, d.Date.ToString(DateFormat), d.StartTime.ToString("HH\\:mm"), d.EndTime.ToString("HH\\:mm"),
            new List<CampDayTrainerDto>())).ToList();

        return Result<PublicCampDto>.Ok(new PublicCampDto(
            camp.Id, camp.Name, camp.Description, camp.Level.HasValue ? (int)camp.Level.Value : null,
            camp.Price, camp.StartDate.ToString(DateFormat), camp.EndDate.ToString(DateFormat),
            camp.RegistrationDeadline, camp.TennisClub?.Name ?? string.Empty,
            camp.MaxParticipants, participants, days));
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
        if (form is not null)
        {
            Error? formError = FormResponseValidator.Validate(
                form.Fields.Select(f => (f.Id, f.IsRequired, f.Label)),
                request.Responses.Select(r => (r.CampFormFieldId, r.Value)));
            if (formError is not null)
                return Result<SubmitCampEnrollmentResultDto>.Fail(formError);
        }

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

        bool isPaid = camp.Price > 0m;
        EnrollmentStatus initialStatus = isPaid ? EnrollmentStatus.PendingPayment : EnrollmentStatus.Confirmed;

        CampEnrollment enrollment;
        try
        {
            await enrollments.BeginTransactionAsync(IsolationLevel.Serializable, ct);

            if (camp.MaxParticipants.HasValue)
            {
                int activeCount = await enrollments.CountActiveByCampAsync(campId, ct);
                if (activeCount + groupSize > camp.MaxParticipants.Value)
                {
                    await enrollments.RollbackTransactionAsync(ct);
                    return Result<SubmitCampEnrollmentResultDto>.Fail(new Error(ErrorCodes.Conflict, "Dit kamp is volzet."));
                }
            }

            bool duplicate = await enrollments.IsDuplicateAsync(campId, request.ParticipantEmail, ct);
            if (duplicate)
            {
                await enrollments.RollbackTransactionAsync(ct);
                return Result<SubmitCampEnrollmentResultDto>.Fail(new Error(ErrorCodes.Conflict, "Je bent al ingeschreven voor dit kamp."));
            }

            if (request.EnrollmentType == "group" && request.GroupMembers is { Count: > 0 })
            {
                foreach (CampGroupMemberDto member in request.GroupMembers)
                {
                    bool memberDuplicate = await enrollments.IsDuplicateAsync(campId, member.ParticipantEmail, ct);
                    if (memberDuplicate)
                    {
                        await enrollments.RollbackTransactionAsync(ct);
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
            };
            await enrollments.AddAsync(enrollment, ct);

            foreach (CampFormResponseValueDto r in request.Responses)
                await enrollments.AddFormResponseAsync(new CampFormResponse
                {
                    CampEnrollmentId = enrollment.Id, CampFormFieldId = r.CampFormFieldId, Value = r.Value,
                }, ct);

            await enrollments.SaveChangesAsync(ct);

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
                await enrollments.SaveChangesAsync(ct);

                enrollment.CampEnrollmentGroupId = group.Id;

                foreach (CampGroupMemberDto member in request.GroupMembers)
                {
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
                    };
                    await enrollments.AddAsync(memberEnrollment, ct);

                    if (member.Responses is { Count: > 0 })
                        foreach (CampFormResponseValueDto r in member.Responses)
                            await enrollments.AddFormResponseAsync(new CampFormResponse
                            {
                                CampEnrollmentId = memberEnrollment.Id, CampFormFieldId = r.CampFormFieldId, Value = r.Value,
                            }, ct);
                }
                await enrollments.SaveChangesAsync(ct);
            }

            await enrollments.CommitTransactionAsync(ct);
        }
        catch (Exception ex)
        {
            await enrollments.RollbackTransactionAsync(ct);
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

    public async Task<Result<CampPaymentOptionsDto>> GetPaymentOptionsAsync(
        Guid campId, CancellationToken ct = default)
    {
        Camp? camp = await camps.GetByIdPublicAsync(campId, ct);
        if (camp is null)
            return Result<CampPaymentOptionsDto>.Fail(new Error(ErrorCodes.NotFound, "Kamp niet gevonden."));

        Result<MollieConnectionStatusDto> statusResult = await mollieConnect.GetStatusAsync(camp.OrganizationId, ct);
        bool onlineAvailable = statusResult.Value?.Connected ?? false;

        return Result<CampPaymentOptionsDto>.Ok(new CampPaymentOptionsDto(camp.Price, onlineAvailable));
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

        Result cashResult = await paymentService.RecordCampCashPaymentAsync(
            campEnrollmentId, enrollment.OrganizationId, ct);
        if (!cashResult.IsSuccess)
            return Result<ChooseCampPaymentResultDto>.Fail(cashResult.Errors);

        return Result<ChooseCampPaymentResultDto>.Ok(new ChooseCampPaymentResultDto(null));
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
