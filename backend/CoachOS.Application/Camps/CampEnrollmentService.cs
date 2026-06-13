using System.Data;
using System.Text.Json;
using CoachOS.Application.Camps.DTOs;
using CoachOS.Application.Common;
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

        // Betaling + mails na commit.
        string? checkoutUrl = null;
        if (isPaid)
        {
            Result<CreatePaymentResultDto> paymentResult = await paymentService.CreatePaymentForCampEnrollmentAsync(
                enrollment.Id, camp.OrganizationId, ct);
            if (!paymentResult.IsSuccess)
            {
                // Inschrijving staat al (PendingPayment); betaling kan later opnieuw via de mail/Mollie.
                logger.LogError("Mollie payment-creatie faalde voor kampinschrijving {Id}", enrollment.Id);
                return Result<SubmitCampEnrollmentResultDto>.Fail(paymentResult.Errors);
            }
            checkoutUrl = paymentResult.Value!.CheckoutUrl;

            await SafeSendAsync(() => emailService.SendCampEnrollmentPaymentLinkAsync(
                request.ParticipantEmail, request.ParticipantName, camp.Name,
                camp.StartDate, camp.EndDate, checkoutUrl, ct), enrollment.Id);
        }
        else
        {
            await SafeSendAsync(() => emailService.SendCampEnrollmentConfirmedAsync(
                request.ParticipantEmail, request.ParticipantName, camp.Name,
                camp.StartDate, camp.EndDate, ct), enrollment.Id);
        }

        return Result<SubmitCampEnrollmentResultDto>.Ok(new SubmitCampEnrollmentResultDto(enrollment.Id, checkoutUrl));
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

    private static List<string>? DeserializeOptions(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try { return JsonSerializer.Deserialize<List<string>>(json); }
        catch (JsonException) { return null; }
    }
}
