using System.Text.Json;
using CoachOS.Application.Enrollments.DTOs;
using CoachOS.Application.LessonSerie.DTOs;
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
    ILessonSerieRepository lessonSeriesRepo,
    IEnrollmentGroupRepository enrollmentGroupRepo,
    ITimeSlotPreferenceRepository timeSlotPreferenceRepo,
    IUserLookupService userLookup,
    IEmailService emailService,
    ApplicationMapper mapper,
    ILogger<EnrollmentService> logger) : IEnrollmentService
{
    public async Task<Result<PublicLessonSerieDto>> GetPublicLessonSerieAsync(
        Guid lessonSeriesId, CancellationToken ct = default)
    {
        var series = await lessonSeriesRepo.GetByIdPublicAsync(lessonSeriesId, ct);

        if (series is null)
            return Result<PublicLessonSerieDto>.Fail(
                new Error(ErrorCodes.NotFound, "Lessenreeks niet gevonden."));

        var enrollmentCount = await enrollmentRepo.CountActiveBySeriesAsync(series.Id, ct);

        var lessons = series.Lessons
            .OrderBy(l => l.Date)
            .ThenBy(l => l.StartTime)
            .Select(l => mapper.ToLessonDto(l, series.Id))
            .ToList();

        PublicLessonSerieDto dto = new()
        {
            Id = series.Id,
            Name = series.Name,
            Description = series.Description,
            Level = series.Level.HasValue ? (int)series.Level.Value : null,
            Price = series.Price,
            StartDate = series.StartDate.ToString("yyyy-MM-dd"),
            EndDate = series.EndDate.ToString("yyyy-MM-dd"),
            RegistrationDeadline = series.RegistrationDeadline,
            TennisClubName = series.TennisClub?.Name ?? string.Empty,
            MaxRegistrations = series.MaxRegistrations,
            EnrollmentCount = enrollmentCount,
            WeeklyTemplate = series.WeeklyTemplate
                .OrderBy(w => w.DayOfWeek)
                .ThenBy(w => w.StartTime)
                .Select(mapper.ToWeeklyTemplateEntryDto)
                .ToList(),
            Lessons = lessons,
        };

        return Result<PublicLessonSerieDto>.Ok(dto);
    }

    public async Task<Result<EnrollmentFormDto?>> GetEnrollmentFormAsync(
        Guid lessonSeriesId, CancellationToken ct = default)
    {
        var form = await enrollmentFormRepo.GetBySeriesIdReadOnlyAsync(lessonSeriesId, ct);

        if (form is null)
            return Result<EnrollmentFormDto?>.Ok(null);

        EnrollmentFormDto dto = new()
        {
            Id = form.Id,
            LessonSerieId = form.LessonSerieId,
            Fields = form.Fields.Select(f => new FormFieldDto
            {
                Id = f.Id,
                Label = f.Label,
                Type = (int)f.Type,
                IsRequired = f.IsRequired,
                Order = f.Order,
                Options = DeserializeOptions(f.Options),
            }).ToList(),
        };

        return Result<EnrollmentFormDto?>.Ok(dto);
    }

    public async Task<Result<List<LessonSerieEnrollmentDto>>> GetSeriesEnrollmentsAsync(
        Guid lessonSeriesId, Guid organizationId, CancellationToken ct = default)
    {
        var enrollments =
            await enrollmentRepo.GetBySeriesAsync(lessonSeriesId, organizationId, ct);

        var dtos = enrollments.Select(e => new LessonSerieEnrollmentDto
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
                    FieldLabel = System.Net.WebUtility.HtmlEncode(r.FormField.Label),
                    Value = System.Net.WebUtility.HtmlEncode(r.Value),
                }).ToList(),
        }).ToList();

        return Result<List<LessonSerieEnrollmentDto>>.Ok(dtos);
    }

    public async Task<Result<Guid>> SaveFormAsync(
        Guid lessonSeriesId, Guid organizationId, SaveEnrollmentFormRequest request, CancellationToken ct = default)
    {
        var seriesExists = await lessonSeriesRepo.ExistsAsync(lessonSeriesId, organizationId, ct);
        if (!seriesExists)
            return Result<Guid>.Fail(new Error(ErrorCodes.NotFound, "Lessenreeks niet gevonden."));

        var form = await enrollmentFormRepo.GetBySeriesIdWithFieldsAsync(lessonSeriesId, ct);

        if (form is null)
        {
            form = new EnrollmentForm
            {
                OrganizationId = organizationId,
                LessonSerieId = lessonSeriesId,
            };
            await enrollmentFormRepo.AddAsync(form, ct);
        }

        // Determine which existing fields to delete
        var incomingIds = request.Fields
            .Where(f => f.Id.HasValue)
            .Select(f => f.Id!.Value)
            .ToList();

        var toDelete = form.Fields
            .Where(f => !incomingIds.Contains(f.Id))
            .ToList();

        foreach (var field in toDelete)
            enrollmentFormRepo.RemoveField(field);

        // Update existing + insert new
        var order = 0;
        foreach (var dto in request.Fields)
        {
            var optionsJson = dto.Type == (int)FormFieldType.MultipleChoice && dto.Options?.Count > 0
                ? JsonSerializer.Serialize(dto.Options)
                : null;

            if (dto.Id.HasValue)
            {
                var existing = form.Fields.FirstOrDefault(f => f.Id == dto.Id.Value);
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
        var series = await lessonSeriesRepo.GetByIdPublicAsync(lessonSeriesId, ct);
        if (series is null)
            return Result<Guid>.Fail(new Error(ErrorCodes.NotFound, "Lessenreeks niet gevonden."));

        // 2. Registration deadline check
        if (DateTime.UtcNow > series.RegistrationDeadline)
            return Result<Guid>.Fail(
                new Error(ErrorCodes.Validation, "De inschrijvingsdeadline is verstreken."));

        // 3. Capacity check (accounts for group size)
        if (series.MaxRegistrations.HasValue)
        {
            var activeCount = await enrollmentRepo.CountActiveBySeriesAsync(lessonSeriesId, ct);
            var groupSize = request.EnrollmentType == "group" && request.GroupMembers is not null
                ? request.GroupMembers.Count + 1
                : 1;
            if (activeCount + groupSize > series.MaxRegistrations.Value)
                return Result<Guid>.Fail(
                    new Error(ErrorCodes.Conflict, "Deze lessenreeks is volzet."));
        }

        // 4. Load enrollment form with fields (may be null)
        var form = await enrollmentFormRepo.GetBySeriesIdReadOnlyAsync(lessonSeriesId, ct);

        // 5. Validate form responses against actual form fields
        if (form is not null)
        {
            var formFieldIds = form.Fields.Select(f => f.Id).ToHashSet();

            // Reject responses referencing fields that don't belong to this form
            foreach (var response in request.Responses)
            {
                if (!formFieldIds.Contains(response.FormFieldId))
                    return Result<Guid>.Fail(
                        new Error(ErrorCodes.Validation, "Ongeldig formulierveld."));
            }

            // Validate required fields have a non-empty response
            var requiredFields = form.Fields
                .Where(f => f.IsRequired)
                .ToList();

            foreach (var requiredField in requiredFields)
            {
                var hasResponse = request.Responses.Any(r =>
                    r.FormFieldId == requiredField.Id && !string.IsNullOrWhiteSpace(r.Value));

                if (!hasResponse)
                    return Result<Guid>.Fail(
                        new Error(ErrorCodes.Validation, $"Veld '{requiredField.Label}' is verplicht."));
            }
        }

        // 6. Duplicate check
        var isDuplicate = await enrollmentRepo.IsDuplicateAsync(lessonSeriesId, request.StudentEmail, ct);
        if (isDuplicate)
            return Result<Guid>.Fail(
                new Error(ErrorCodes.Conflict, "Je bent al ingeschreven voor deze lessenreeks."));

        // 7. Begin transaction for multi-step enrollment
        Enrollment enrollment;
        await enrollmentRepo.BeginTransactionAsync(ct);
        try
        {

        // 8. Create enrollment
        enrollment = new()
        {
            OrganizationId = series.OrganizationId,
            LessonSerieId = series.Id,
            StudentName = request.StudentName,
            StudentEmail = request.StudentEmail,
            StudentPhone = request.StudentPhone,
            Status = EnrollmentStatus.Confirmed,
            EnrolledAt = DateTime.UtcNow,
            IsOpenToGrouping = request.IsOpenToGrouping,
        };

        await enrollmentRepo.AddAsync(enrollment, ct);

        // 8. Create form responses
        foreach (var responseDto in request.Responses)
        {
            FormResponse response = new()
            {
                EnrollmentId = enrollment.Id,
                FormFieldId = responseDto.FormFieldId,
                Value = responseDto.Value,
            };
            await enrollmentRepo.AddFormResponseAsync(response, ct);
        }

        // 9. Save enrollment + form responses first (needed before group creation to avoid circular FK)
        await enrollmentRepo.SaveChangesAsync(ct);

        // 10. Group enrollment: create group + member enrollments
        if (request.EnrollmentType == "group" && request.GroupMembers is { Count: > 0 })
        {
            var existingGroupCount = await enrollmentGroupRepo.CountBySeriesAsync(
                lessonSeriesId, series.OrganizationId, ct);
            var groupLetter = (char)('A' + existingGroupCount);

            EnrollmentGroup group = new()
            {
                OrganizationId = series.OrganizationId,
                LessonSerieId = series.Id,
                Name = $"Groep {groupLetter}",
                LeaderEnrollmentId = enrollment.Id,
            };

            await enrollmentGroupRepo.AddAsync(group, ct);
            await enrollmentGroupRepo.SaveChangesAsync(ct);

            enrollment.EnrollmentGroupId = group.Id;

            foreach (var member in request.GroupMembers)
            {
                Enrollment memberEnrollment = new()
                {
                    OrganizationId = series.OrganizationId,
                    LessonSerieId = series.Id,
                    StudentName = member.StudentName,
                    StudentEmail = member.StudentEmail,
                    StudentPhone = member.StudentPhone,
                    Status = EnrollmentStatus.Confirmed,
                    EnrolledAt = DateTime.UtcNow,
                    EnrollmentGroupId = group.Id,
                };

                await enrollmentRepo.AddAsync(memberEnrollment, ct);

                if (member.Responses is { Count: > 0 })
                {
                    foreach (var responseDto in member.Responses)
                    {
                        FormResponse response = new()
                        {
                            EnrollmentId = memberEnrollment.Id,
                            FormFieldId = responseDto.FormFieldId,
                            Value = responseDto.Value,
                        };
                        await enrollmentRepo.AddFormResponseAsync(response, ct);
                    }
                }
            }

            await enrollmentRepo.SaveChangesAsync(ct);
        }

        // 12. Save time slot preferences
        if (request.TimeSlotPreferences is { Count: > 0 })
        {
            var preferences = request.TimeSlotPreferences.Select(p => new TimeSlotPreference
            {
                OrganizationId = series.OrganizationId,
                EnrollmentId = enrollment.Id,
                WeeklyTemplateEntryId = p.WeeklyTemplateEntryId,
                Preference = (SlotPreference)p.Preference,
            });

            await timeSlotPreferenceRepo.AddRangeAsync(preferences, ct);
            await timeSlotPreferenceRepo.SaveChangesAsync(ct);
        }

        await enrollmentRepo.CommitTransactionAsync(ct);

        }
        catch (Exception ex)
        {
            await enrollmentRepo.RollbackTransactionAsync(ct);
            logger.LogError(ex, "Inschrijving mislukt voor reeks {SeriesId}", lessonSeriesId);
            return Result<Guid>.Fail(new Error(ErrorCodes.Unexpected, "Inschrijving mislukt. Probeer het opnieuw."));
        }

        // 13. Send notification emails (fire-and-forget in try/catch)
        try
        {
            var firstTrainerId = series.Lessons
                .OrderBy(l => l.Date).ThenBy(l => l.StartTime)
                .Select(l => l.TrainerId)
                .FirstOrDefault(id => id.HasValue);

            var trainerInfo = firstTrainerId.HasValue
                ? await userLookup.GetUserInfoByIdAsync(firstTrainerId.Value, ct)
                : null;

            List<(string FieldLabel, string Value)> responseItems = new();
            if (form is not null)
            {
                foreach (var r in request.Responses)
                {
                    var field = form.Fields.FirstOrDefault(f => f.Id == r.FormFieldId);
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

    public async Task<Result<List<PublicTimeSlotDto>>> GetPublicTimeSlotsAsync(
        Guid lessonSeriesId, CancellationToken ct = default)
    {
        var series = await lessonSeriesRepo.GetByIdPublicAsync(lessonSeriesId, ct);

        if (series is null)
            return Result<List<PublicTimeSlotDto>>.Fail(
                new Error(ErrorCodes.NotFound, "Lessenreeks niet gevonden."));

        var slots = series.WeeklyTemplate
            .OrderBy(w => w.DayOfWeek)
            .ThenBy(w => w.StartTime)
            .Select(mapper.ToPublicTimeSlotDto)
            .ToList();

        return Result<List<PublicTimeSlotDto>>.Ok(slots);
    }

    public async Task<Result<List<EnrollmentWithPreferencesDto>>> GetSeriesEnrollmentsWithPreferencesAsync(
        Guid lessonSeriesId, Guid organizationId, CancellationToken ct = default)
    {
        var exists = await lessonSeriesRepo.ExistsAsync(lessonSeriesId, organizationId, ct);
        if (!exists)
            return Result<List<EnrollmentWithPreferencesDto>>.Fail(
                new Error(ErrorCodes.NotFound, "Lessenreeks niet gevonden."));

        var enrollments = await enrollmentRepo.GetBySeriesAsync(lessonSeriesId, organizationId, ct);
        var preferences = await timeSlotPreferenceRepo.GetBySeriesAsync(lessonSeriesId, organizationId, ct);
        var groups = await enrollmentGroupRepo.GetBySeriesAsync(lessonSeriesId, organizationId, ct);

        var prefsByEnrollment = preferences
            .GroupBy(p => p.EnrollmentId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var groupsById = groups.ToDictionary(g => g.Id);

        var dtos = enrollments
            .Where(e => e.Status == EnrollmentStatus.Confirmed || e.Status == EnrollmentStatus.Pending)
            .Select(e =>
            {
                var enrollmentPrefs = prefsByEnrollment.GetValueOrDefault(e.Id, []);
                EnrollmentGroup? group = e.EnrollmentGroupId.HasValue
                    ? groupsById.GetValueOrDefault(e.EnrollmentGroupId.Value)
                    : null;

                return new EnrollmentWithPreferencesDto
                {
                    Id = e.Id,
                    StudentName = e.StudentName,
                    StudentEmail = e.StudentEmail,
                    Status = e.Status.ToString(),
                    IsOpenToGrouping = e.IsOpenToGrouping,
                    EnrollmentGroupId = e.EnrollmentGroupId,
                    GroupName = group?.Name,
                    IsGroupLeader = group?.LeaderEnrollmentId == e.Id,
                    Preferences = enrollmentPrefs.Select(p => new TimeSlotPreferenceDto
                    {
                        WeeklyTemplateEntryId = p.WeeklyTemplateEntryId,
                        Preference = (int)p.Preference,
                    }).ToList(),
                };
            })
            .ToList();

        return Result<List<EnrollmentWithPreferencesDto>>.Ok(dtos);
    }

    private List<string>? DeserializeOptions(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Ongeldige JSON in formulierveld opties: {Json}", json);
            return null;
        }
    }
}
