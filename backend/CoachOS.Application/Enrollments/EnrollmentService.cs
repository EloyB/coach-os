using System.Text.Json;
using CoachOS.Application.Enrollments.DTOs;
using CoachOS.Application.LessonSeries.DTOs;
using CoachOS.Application.Mappings;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using Microsoft.Extensions.Logging;

namespace CoachOS.Application.Enrollments;

public class EnrollmentService(
    IEnrollmentRepository enrollmentRepo,
    IEnrollmentFormRepository enrollmentFormRepo,
    ILessonSeriesRepository lessonSeriesRepo,
    IUserLookupService userLookup,
    IEmailService emailService,
    ApplicationMapper mapper,
    ILogger<EnrollmentService> logger) : IEnrollmentService
{
    public async Task<Result<PublicLessonSeriesDto>> GetPublicLessonSeriesAsync(
        Guid lessonSeriesId, CancellationToken ct = default)
    {
        Domain.Entities.LessonSeries? series = await lessonSeriesRepo.GetByIdPublicAsync(lessonSeriesId, ct);

        if (series is null)
            return Result<PublicLessonSeriesDto>.Fail(
                new Error(ErrorCodes.NotFound, "Lessenreeks niet gevonden."));

        string trainerName = await userLookup.GetUserNameByIdAsync(series.TrainerId, ct) ?? string.Empty;
        int enrollmentCount = await enrollmentRepo.CountActiveBySeriesAsync(series.Id, ct);

        List<LessonDto> lessons = series.Lessons
            .OrderBy(l => l.Date)
            .ThenBy(l => l.StartTime)
            .Select(l => mapper.ToLessonDto(l, series.Id))
            .ToList();

        PublicLessonSeriesDto dto = new()
        {
            Id = series.Id,
            Name = series.Name,
            Description = series.Description,
            TrainerName = trainerName,
            Level = (int)series.Level,
            Price = series.Price,
            StartDate = series.StartDate.ToString("yyyy-MM-dd"),
            EndDate = series.EndDate.ToString("yyyy-MM-dd"),
            DurationMinutes = series.DurationMinutes,
            TennisClubName = series.TennisClub?.Name ?? string.Empty,
            EnrollmentCount = enrollmentCount,
            Lessons = lessons,
        };

        return Result<PublicLessonSeriesDto>.Ok(dto);
    }

    public async Task<Result<EnrollmentFormDto?>> GetEnrollmentFormAsync(
        Guid lessonSeriesId, CancellationToken ct = default)
    {
        EnrollmentForm? form = await enrollmentFormRepo.GetBySeriesIdReadOnlyAsync(lessonSeriesId, ct);

        if (form is null)
            return Result<EnrollmentFormDto?>.Ok(null);

        EnrollmentFormDto dto = new()
        {
            Id = form.Id,
            LessonSeriesId = form.LessonSeriesId,
            Fields = form.Fields.Select(f => new FormFieldDto
            {
                Id = f.Id,
                Label = f.Label,
                Type = (int)f.Type,
                IsRequired = f.IsRequired,
                Order = f.Order,
                Options = f.Options is not null
                    ? JsonSerializer.Deserialize<List<string>>(f.Options)
                    : null,
            }).ToList(),
        };

        return Result<EnrollmentFormDto?>.Ok(dto);
    }

    public async Task<Result<List<LessonSeriesEnrollmentDto>>> GetSeriesEnrollmentsAsync(
        Guid lessonSeriesId, Guid organizationId, CancellationToken ct = default)
    {
        List<Enrollment> enrollments =
            await enrollmentRepo.GetBySeriesAsync(lessonSeriesId, organizationId, ct);

        List<LessonSeriesEnrollmentDto> dtos = enrollments.Select(e => new LessonSeriesEnrollmentDto
        {
            Id = e.Id,
            StudentName = e.StudentName,
            StudentEmail = e.StudentEmail,
            Status = e.Status.ToString(),
            EnrolledAt = e.EnrolledAt,
            Notes = e.Notes,
            FormResponses = e.FormResponses
                .OrderBy(r => r.FormField.Order)
                .Select(r => new EnrollmentResponseItemDto
                {
                    FieldLabel = r.FormField.Label,
                    Value = r.Value,
                }).ToList(),
        }).ToList();

        return Result<List<LessonSeriesEnrollmentDto>>.Ok(dtos);
    }

    public async Task<Result<Guid>> SaveFormAsync(
        Guid lessonSeriesId, Guid organizationId, SaveEnrollmentFormRequest request, CancellationToken ct = default)
    {
        bool seriesExists = await lessonSeriesRepo.ExistsAsync(lessonSeriesId, organizationId, ct);
        if (!seriesExists)
            return Result<Guid>.Fail(new Error(ErrorCodes.NotFound, "Lessenreeks niet gevonden."));

        EnrollmentForm? form = await enrollmentFormRepo.GetBySeriesIdWithFieldsAsync(lessonSeriesId, ct);

        if (form is null)
        {
            form = new EnrollmentForm
            {
                OrganizationId = organizationId,
                LessonSeriesId = lessonSeriesId,
            };
            await enrollmentFormRepo.AddAsync(form, ct);
        }

        // Determine which existing fields to delete
        List<Guid> incomingIds = request.Fields
            .Where(f => f.Id.HasValue)
            .Select(f => f.Id!.Value)
            .ToList();

        List<FormField> toDelete = form.Fields
            .Where(f => !incomingIds.Contains(f.Id))
            .ToList();

        foreach (FormField field in toDelete)
            enrollmentFormRepo.RemoveField(field);

        // Update existing + insert new
        int order = 0;
        foreach (SaveFormFieldRequest dto in request.Fields)
        {
            string? optionsJson = dto.Type == (int)FormFieldType.MultipleChoice && dto.Options?.Count > 0
                ? JsonSerializer.Serialize(dto.Options)
                : null;

            if (dto.Id.HasValue)
            {
                FormField? existing = form.Fields.FirstOrDefault(f => f.Id == dto.Id.Value);
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
                FormField newField = new()
                {
                    EnrollmentFormId = form.Id,
                    Label = dto.Label,
                    Type = (FormFieldType)dto.Type,
                    IsRequired = dto.IsRequired,
                    Order = order,
                    Options = optionsJson,
                };
                form.Fields.Add(newField);
            }

            order++;
        }

        await enrollmentFormRepo.SaveChangesAsync(ct);
        return Result<Guid>.Ok(form.Id);
    }

    public async Task<Result<Guid>> SubmitEnrollmentAsync(
        Guid lessonSeriesId, SubmitEnrollmentRequest request, CancellationToken ct = default)
    {
        // 1. Load active lesson series
        Domain.Entities.LessonSeries? series = await lessonSeriesRepo.GetByIdPublicAsync(lessonSeriesId, ct);
        if (series is null)
            return Result<Guid>.Fail(new Error(ErrorCodes.NotFound, "Lessenreeks niet gevonden."));

        // 2. Load enrollment form with fields (may be null)
        EnrollmentForm? form = await enrollmentFormRepo.GetBySeriesIdReadOnlyAsync(lessonSeriesId, ct);

        // 3. Validate required custom fields
        if (form is not null)
        {
            List<FormField> requiredFields = form.Fields
                .Where(f => f.IsRequired)
                .ToList();

            foreach (FormField requiredField in requiredFields)
            {
                bool hasResponse = request.Responses.Any(r =>
                    r.FormFieldId == requiredField.Id && !string.IsNullOrWhiteSpace(r.Value));

                if (!hasResponse)
                    return Result<Guid>.Fail(
                        new Error(ErrorCodes.Validation, $"Veld '{requiredField.Label}' is verplicht"));
            }
        }

        // 4. Duplicate check
        bool isDuplicate = await enrollmentRepo.IsDuplicateAsync(lessonSeriesId, request.StudentEmail, ct);
        if (isDuplicate)
            return Result<Guid>.Fail(
                new Error(ErrorCodes.Conflict, "Je bent al ingeschreven voor deze lessenreeks"));

        // 5. Create enrollment
        Enrollment enrollment = new()
        {
            OrganizationId = series.OrganizationId,
            LessonSeriesId = series.Id,
            StudentName = request.StudentName,
            StudentEmail = request.StudentEmail,
            Status = EnrollmentStatus.Confirmed,
            EnrolledAt = DateTime.UtcNow,
        };

        await enrollmentRepo.AddAsync(enrollment, ct);

        // 6. Create form responses
        foreach (FormResponseValueDto responseDto in request.Responses)
        {
            FormResponse response = new()
            {
                EnrollmentId = enrollment.Id,
                FormFieldId = responseDto.FormFieldId,
                Value = responseDto.Value,
            };
            await enrollmentRepo.AddFormResponseAsync(response, ct);
        }

        await enrollmentRepo.SaveChangesAsync(ct);

        // 7. Send notification emails (fire-and-forget in try/catch)
        try
        {
            (string FullName, string Email)? trainerInfo =
                await userLookup.GetUserInfoByIdAsync(series.TrainerId, ct);

            List<(string FieldLabel, string Value)> responseItems = new();
            if (form is not null)
            {
                foreach (FormResponseValueDto r in request.Responses)
                {
                    FormField? field = form.Fields.FirstOrDefault(f => f.Id == r.FormFieldId);
                    if (field is not null)
                        responseItems.Add((field.Label, r.Value));
                }
            }

            await emailService.SendEnrollmentConfirmationAsync(
                request.StudentEmail,
                request.StudentName,
                series.Name,
                trainerInfo?.FullName ?? string.Empty,
                ct);

            if (trainerInfo.HasValue)
            {
                await emailService.SendEnrollmentNotificationToTrainerAsync(
                    trainerInfo.Value.Email,
                    trainerInfo.Value.FullName,
                    request.StudentName,
                    request.StudentEmail,
                    series.Name,
                    responseItems,
                    ct);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "E-mailnotificatie mislukt voor inschrijving {EnrollmentId}", enrollment.Id);
        }

        return Result<Guid>.Ok(enrollment.Id);
    }
}
