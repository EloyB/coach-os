using CoachOS.Application.Abstractions;
using System.Data;
using System.Text.Json;
using CoachOS.Application.Common;
using CoachOS.Application.Enrollments.DTOs;
using CoachOS.Application.LessonSerie.DTOs;
using CoachOS.Application.Mappings;
using CoachOS.Application.Planning;
using CoachOS.Application.Pricing;
using CoachOS.Domain.Common;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using Microsoft.Extensions.Logging;

// Voorkomt ambiguity met de gelijknamige namespace CoachOS.Application.OrganizationSettings.
using OrganizationSettingsEntity = CoachOS.Domain.Entities.OrganizationSettings;

namespace CoachOS.Application.Enrollments;

public partial class EnrollmentService(
    IEnrollmentRepository enrollmentRepo,
    IEnrollmentFormRepository enrollmentFormRepo,
    ILessonSerieRepository lessonSeriesRepo,
    IEnrollmentGroupRepository enrollmentGroupRepo,
    IPaymentRepository paymentRepo,
    ITimeSlotPreferenceRepository timeSlotPreferenceRepo,
    IOrganizationSettingsRepository orgSettingsRepo,
    IUserLookupService userLookup,
    IEmailOutboxRepository emailOutboxRepository,
    ILessonSeriePriceRepository priceRepo,
    IScheduleAssignmentRepository scheduleAssignmentRepo,
    IUnitOfWork unitOfWork,
    ApplicationMapper mapper,
    ILogger<EnrollmentService> logger,
    TimeProvider timeProvider) : IEnrollmentService
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
            MinAge = series.MinAge,
            MaxAge = series.MaxAge,
            EnrollmentCount = enrollmentCount,
            MinimumAvailableSlots = series.MinimumAvailableSlots,
            MaxGroupSize = GroupSizeRules.SeriesMax(series.WeeklyTemplate.Select(w => w.MaxStudents)),
            AllowSoloEnrollment = series.AllowSoloEnrollment,
            AllowGroupEnrollment = series.AllowGroupEnrollment,
            PriceOptions = series.Prices
                .OrderBy(p => p.SortOrder)
                .Select(LessonSeriePricingService.ToDto)
                .ToList(),
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

        var groups = await enrollmentGroupRepo.GetBySeriesAsync(lessonSeriesId, organizationId, ct);
        var groupsById = groups.ToDictionary(g => g.Id);

        // Eigen betaalmethode + status per inschrijving. Bij per-lid cash-betalingen
        // heeft elk lid een eigen betaling; bij een online-groep enkel de leider.
        var paymentByEnrollment = await paymentRepo.GetLatestByEnrollmentIdsAsync(
            enrollments.Select(e => e.Id), ct);

        // Solo: id van de inschrijving; groep: id van de groep (één toewijzing per groep).
        var awaitingConfirmation = await scheduleAssignmentRepo.GetAwaitingConfirmationParticipantIdsAsync(
            lessonSeriesId, organizationId, ct);

        var dtos = enrollments.Select(e => new LessonSerieEnrollmentDto
        {
            Id = e.Id,
            StudentName = e.StudentName,
            StudentEmail = e.StudentEmail,
            StudentPhone = e.StudentPhone,
            ContactEmail = e.ContactEmail,
            HasOwnEmail = e.StudentEmail is not null,
            Status = e.Status.ToString(),
            EnrolledAt = e.EnrolledAt,
            Notes = e.Notes,
            DateOfBirth = e.DateOfBirth?.ToString("yyyy-MM-dd"),
            Category = e.Category.HasValue ? (int)e.Category.Value : null,
            CategoryLabel = e.Category switch
            {
                ParticipantCategory.Youth => "Jeugd",
                ParticipantCategory.Adult => "Volwassenen",
                _ => null,
            },
            EnrollmentGroupId = e.EnrollmentGroupId,
            IsGroupLeader = e.EnrollmentGroupId.HasValue
                && groupsById.GetValueOrDefault(e.EnrollmentGroupId.Value)?.LeaderEnrollmentId == e.Id,
            MaxGroupSize = e.MaxGroupSize,
            SelectedPriceOptionId = e.SelectedPriceOptionId,
            PaymentMethod = paymentByEnrollment.GetValueOrDefault(e.Id)?.Method?.ToString(),
            PaymentStatus = paymentByEnrollment.GetValueOrDefault(e.Id)?.Status.ToString(),
            PaymentAmount = paymentByEnrollment.GetValueOrDefault(e.Id)?.Amount,
            PaidAt = paymentByEnrollment.GetValueOrDefault(e.Id)?.PaidAt,
            MolliePaymentId = paymentByEnrollment.GetValueOrDefault(e.Id)?.MolliePaymentId,
            IsAwaitingConfirmation = awaitingConfirmation.Contains(e.EnrollmentGroupId ?? e.Id),
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

        await unitOfWork.SaveChangesAsync(ct);
        return Result<Guid>.Ok(form.Id);
    }

    public async Task<Result<Guid>> CreateManualEnrollmentAsync(
        Guid lessonSeriesId, CreateManualEnrollmentRequest request, Guid organizationId,
        CancellationToken ct = default)
    {
        Domain.Entities.LessonSerie? series = await lessonSeriesRepo.GetByIdPublicAsync(lessonSeriesId, ct);
        if (series is null || series.OrganizationId != organizationId)
            return Result<Guid>.Fail(new Error(ErrorCodes.NotFound, "Lessenreeks niet gevonden."));

        if (!DateOfBirthRules.TryParse(request.DateOfBirth, out DateOnly dateOfBirth))
            return Result<Guid>.Fail(new Error(ErrorCodes.Validation, "Geboortedatum is ongeldig."));

        Error? ageError = CheckAgeEligibility(
            new SubmitEnrollmentRequest { StudentName = request.StudentName, DateOfBirth = request.DateOfBirth }, series);
        if (ageError is not null)
            return Result<Guid>.Fail(ageError);

        // Maximum aantal personen per tijdslot: standaard solo (1), tenzij de admin een hoger maximum kiest.
        int maxGroupSize = request.MaxGroupSize ?? 1;
        Error? maxGroupSizeError = GroupSizeRules.Validate(
            maxGroupSize, ownSize: 1, GroupSizeRules.SeriesMax(series.WeeklyTemplate.Select(w => w.MaxStudents)));
        if (maxGroupSizeError is not null)
            return Result<Guid>.Fail(maxGroupSizeError);

        EnrollmentForm? form = await enrollmentFormRepo.GetBySeriesIdReadOnlyAsync(lessonSeriesId, ct);
        if (form is not null)
        {
            Error? formError = FormResponseValidator.Validate(
                form.Fields.Select(f => (f.Id, f.IsRequired, f.Label)),
                request.Responses.Select(r => (r.FormFieldId, r.Value)));
            if (formError is not null) return Result<Guid>.Fail(formError);
        }

        OrganizationSettingsEntity? settings =
            await orgSettingsRepo.GetByOrganizationReadOnlyAsync(organizationId, ct);
        int youthMaxAge = settings?.YouthMaxAge ?? 17;
        string contactEmail = EnrollmentEmails.Normalize(request.ContactEmail);

        await unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            if (series.MaxRegistrations.HasValue &&
                await enrollmentRepo.CountActiveBySeriesAsync(lessonSeriesId, ct) >= series.MaxRegistrations.Value)
            {
                await unitOfWork.RollbackTransactionAsync(ct);
                return Result<Guid>.Fail(new Error(ErrorCodes.Conflict, "Deze lessenreeks is volzet."));
            }

            if (await enrollmentRepo.IsDuplicateParticipantAsync(
                    lessonSeriesId, contactEmail, request.StudentName, dateOfBirth, ct))
            {
                await unitOfWork.RollbackTransactionAsync(ct);
                return Result<Guid>.Fail(new Error(
                    ErrorCodes.Conflict, $"{request.StudentName} is al ingeschreven voor deze lessenreeks."));
            }

            Enrollment enrollment = new()
            {
                OrganizationId = organizationId,
                LessonSerieId = lessonSeriesId,
                StudentName = request.StudentName.Trim(),
                ContactEmail = contactEmail,
                StudentEmail = string.IsNullOrWhiteSpace(request.StudentEmail)
                    ? null
                    : EnrollmentEmails.Normalize(request.StudentEmail),
                StudentPhone = request.StudentPhone,
                DateOfBirth = dateOfBirth,
                Category = ResolveCategory(request.DateOfBirth, youthMaxAge, timeProvider.GetBrusselsToday()),
                Status = EnrollmentStatus.Confirmed,
                EnrolledAt = timeProvider.GetUtcNow().UtcDateTime,
                MaxGroupSize = maxGroupSize,
            };
            await enrollmentRepo.AddAsync(enrollment, ct);

            foreach (FormResponseValueDto responseDto in request.Responses)
            {
                await enrollmentRepo.AddFormResponseAsync(new FormResponse
                {
                    EnrollmentId = enrollment.Id,
                    FormFieldId = responseDto.FormFieldId,
                    Value = responseDto.Value,
                }, ct);
            }

            await unitOfWork.SaveChangesAsync(ct);
            await emailOutboxRepository.AddRangeAsync([
                new EmailOutboxMessage
                {
                    OrganizationId = organizationId,
                    EnrollmentId = enrollment.Id,
                    Type = EmailOutboxMessageTypes.EnrollmentConfirmation,
                    Payload = JsonSerializer.Serialize(new EnrollmentConfirmationEmailPayload(
                        contactEmail, enrollment.StudentName, series.Name,
                        [enrollment.StudentName])),
                }
            ], ct);
            await unitOfWork.SaveChangesAsync(ct);
            await unitOfWork.CommitTransactionAsync(ct);
            return Result<Guid>.Ok(enrollment.Id);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }
    }

    public async Task<Result<Guid>> AddGroupMemberAsync(
        Guid lessonSeriesId, Guid groupId, CreateManualEnrollmentRequest request,
        Guid organizationId, CancellationToken ct = default)
    {
        Domain.Entities.LessonSerie? series = await lessonSeriesRepo.GetByIdPublicAsync(lessonSeriesId, ct);
        if (series is null || series.OrganizationId != organizationId)
            return Result<Guid>.Fail(new Error(ErrorCodes.NotFound, "Lessenreeks niet gevonden."));

        Domain.Entities.EnrollmentGroup? group = await enrollmentGroupRepo.GetByIdAsync(groupId, organizationId, ct);
        if (group is null || group.LessonSerieId != lessonSeriesId)
            return Result<Guid>.Fail(new Error(ErrorCodes.NotFound, "Groep niet gevonden."));

        Domain.Entities.Enrollment? leader =
            group.Members.FirstOrDefault(m => m.Id == group.LeaderEnrollmentId)
            ?? group.Members.FirstOrDefault();
        if (leader is null)
            return Result<Guid>.Fail(new Error(ErrorCodes.NotFound, "Deze groep heeft geen leden."));

        // Gate: geen lid toevoegen aan een al betaalde/bevestigde groep.
        if (leader.Status is EnrollmentStatus.Confirmed or EnrollmentStatus.PendingPayment)
            return Result<Guid>.Fail(new Error(ErrorCodes.Conflict,
                "Je kan geen lid toevoegen aan een groep die al betaald of bevestigd is."));

        if (!DateOfBirthRules.TryParse(request.DateOfBirth, out DateOnly dateOfBirth))
            return Result<Guid>.Fail(new Error(ErrorCodes.Validation, "Geboortedatum is ongeldig."));

        Error? ageError = CheckAgeEligibility(
            new SubmitEnrollmentRequest { StudentName = request.StudentName, DateOfBirth = request.DateOfBirth }, series);
        if (ageError is not null)
            return Result<Guid>.Fail(ageError);

        EnrollmentForm? form = await enrollmentFormRepo.GetBySeriesIdReadOnlyAsync(lessonSeriesId, ct);
        if (form is not null)
        {
            Error? formError = FormResponseValidator.Validate(
                form.Fields.Select(f => (f.Id, f.IsRequired, f.Label)),
                request.Responses.Select(r => (r.FormFieldId, r.Value)));
            if (formError is not null) return Result<Guid>.Fail(formError);
        }

        OrganizationSettingsEntity? settings =
            await orgSettingsRepo.GetByOrganizationReadOnlyAsync(organizationId, ct);
        int youthMaxAge = settings?.YouthMaxAge ?? 17;
        string contactEmail = EnrollmentEmails.Normalize(request.ContactEmail);

        await unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            if (series.MaxRegistrations.HasValue &&
                await enrollmentRepo.CountActiveBySeriesAsync(lessonSeriesId, ct) >= series.MaxRegistrations.Value)
            {
                await unitOfWork.RollbackTransactionAsync(ct);
                return Result<Guid>.Fail(new Error(ErrorCodes.Conflict, "Deze lessenreeks is volzet."));
            }

            if (await enrollmentRepo.IsDuplicateParticipantAsync(
                    lessonSeriesId, contactEmail, request.StudentName, dateOfBirth, ct))
            {
                await unitOfWork.RollbackTransactionAsync(ct);
                return Result<Guid>.Fail(new Error(
                    ErrorCodes.Conflict, $"{request.StudentName} is al ingeschreven voor deze lessenreeks."));
            }

            // Het lid erft het maximum van de groep. Wordt de groep door dit lid groter dan dat maximum, dan gaat het
            // maximum voor iedereen mee omhoog: een groep kan niet groter zijn dan het maximum dat ze zelf koos.
            int newGroupSize = group.Members.Count(m =>
                m.Status is EnrollmentStatus.Confirmed or EnrollmentStatus.Pending or EnrollmentStatus.PendingPayment) + 1;
            int groupMaxGroupSize = Math.Max(leader.MaxGroupSize, newGroupSize);
            if (groupMaxGroupSize != leader.MaxGroupSize)
            {
                foreach (Enrollment existingMember in group.Members)
                {
                    existingMember.MaxGroupSize = groupMaxGroupSize;
                    existingMember.UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;
                }
            }

            Enrollment enrollment = new()
            {
                OrganizationId = organizationId,
                LessonSerieId = lessonSeriesId,
                EnrollmentGroupId = groupId,
                StudentName = request.StudentName.Trim(),
                ContactEmail = contactEmail,
                StudentEmail = string.IsNullOrWhiteSpace(request.StudentEmail)
                    ? null
                    : EnrollmentEmails.Normalize(request.StudentEmail),
                StudentPhone = request.StudentPhone,
                DateOfBirth = dateOfBirth,
                Category = ResolveCategory(request.DateOfBirth, youthMaxAge, timeProvider.GetBrusselsToday()),
                Status = leader.Status,                                // status geërfd van de groep
                SelectedPriceOptionId = leader.SelectedPriceOptionId,  // prijsoptie geërfd
                EnrolledAt = timeProvider.GetUtcNow().UtcDateTime,
                MaxGroupSize = groupMaxGroupSize,                      // maximum geërfd van de groep
            };
            await enrollmentRepo.AddAsync(enrollment, ct);

            foreach (FormResponseValueDto responseDto in request.Responses)
            {
                await enrollmentRepo.AddFormResponseAsync(new FormResponse
                {
                    EnrollmentId = enrollment.Id,
                    FormFieldId = responseDto.FormFieldId,
                    Value = responseDto.Value,
                }, ct);
            }

            await unitOfWork.SaveChangesAsync(ct);
            // Informatieve "toegevoegd aan groep"-mail (geen bevestiging/betaling — het lid is nog
            // Pending; de bevestigings-/betaalmail volgt bij de normale groeps-bevestiging).
            await emailOutboxRepository.AddRangeAsync([
                new EmailOutboxMessage
                {
                    OrganizationId = organizationId,
                    EnrollmentId = enrollment.Id,
                    Type = EmailOutboxMessageTypes.GroupMemberAdded,
                    Payload = JsonSerializer.Serialize(new GroupMemberAddedEmailPayload(
                        contactEmail, enrollment.StudentName, series.Name,
                        $"Groep van {leader.StudentName}")),
                }
            ], ct);
            await unitOfWork.SaveChangesAsync(ct);
            await unitOfWork.CommitTransactionAsync(ct);
            return Result<Guid>.Ok(enrollment.Id);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }
    }

    public async Task<Result<List<PublicTimeSlotDto>>> GetPublicTimeSlotsAsync(
        Guid lessonSeriesId, CancellationToken ct = default)
    {
        var series = await lessonSeriesRepo.GetByIdPublicForTimeSlotsAsync(lessonSeriesId, ct);

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
            .Where(e => e.Status == EnrollmentStatus.Confirmed
                || e.Status == EnrollmentStatus.Pending
                || e.Status == EnrollmentStatus.PendingPayment)
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
                    // Contactadres: deze DTO voedt de planning, waar dit het verzendadres is.
                    StudentEmail = e.ContactEmail,
                    Status = e.Status.ToString(),
                    MaxGroupSize = e.MaxGroupSize,
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

    public async Task<Result<LessonSerieEnrollmentDto>> UpdateBasicEnrollmentAsync(
        Guid lessonSeriesId, Guid enrollmentId, Guid organizationId,
        UpdateBasicEnrollmentRequest request, CancellationToken ct = default)
    {
        Enrollment? enrollment = await enrollmentRepo.GetByIdAsync(enrollmentId, organizationId, ct);
        if (enrollment is null || enrollment.LessonSerieId != lessonSeriesId)
            return Result<LessonSerieEnrollmentDto>.Fail(
                new Error(ErrorCodes.NotFound, "Inschrijving niet gevonden."));

        DateOnly? dateOfBirth = ParseBirthDate(request.DateOfBirth);
        string contactEmail = EnrollmentEmails.Normalize(request.ContactEmail);
        string studentEmail = string.IsNullOrWhiteSpace(request.StudentEmail)
            ? string.Empty
            : EnrollmentEmails.Normalize(request.StudentEmail);
        string studentName = request.StudentName.Trim();

        bool duplicate = await enrollmentRepo.IsDuplicateParticipantExceptAsync(
            lessonSeriesId, enrollmentId, contactEmail, studentName, dateOfBirth, ct);
        if (duplicate)
            return Result<LessonSerieEnrollmentDto>.Fail(new Error(
                ErrorCodes.Conflict, $"{studentName} is al ingeschreven voor deze lessenreeks."));

        OrganizationSettingsEntity? orgSettings =
            await orgSettingsRepo.GetByOrganizationReadOnlyAsync(organizationId, ct);
        int youthMaxAge = orgSettings?.YouthMaxAge ?? 17;
        DateOnly updatedOn = timeProvider.GetBrusselsToday();

        Error? maxGroupSizeError = await ApplyMaxGroupSizeAsync(
            enrollment, request.MaxGroupSize, lessonSeriesId, organizationId, ct);
        if (maxGroupSizeError is not null)
            return Result<LessonSerieEnrollmentDto>.Fail(maxGroupSizeError);

        enrollment.StudentName = studentName;
        enrollment.ContactEmail = contactEmail;
        enrollment.StudentEmail = string.IsNullOrWhiteSpace(studentEmail) ? null : studentEmail;
        enrollment.StudentPhone = string.IsNullOrWhiteSpace(request.StudentPhone)
            ? null
            : request.StudentPhone.Trim();
        enrollment.DateOfBirth = dateOfBirth;
        enrollment.Category = ResolveCategory(request.DateOfBirth, youthMaxAge, updatedOn);
        enrollment.UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;

        // Prijsoptie: enkel behandelen wanneer ze effectief wijzigt.
        if (request.SelectedPriceOptionId != enrollment.SelectedPriceOptionId)
        {
            // Gate: niet meer aanpasbaar zodra betaald/bevestigd of een betaling loopt.
            if (enrollment.Status is EnrollmentStatus.Confirmed
                or EnrollmentStatus.PendingPayment or EnrollmentStatus.Cancelled)
                return Result<LessonSerieEnrollmentDto>.Fail(new Error(ErrorCodes.Conflict,
                    "De prijsoptie kan niet meer aangepast worden: deze inschrijving is al betaald, bevestigd of geannuleerd."));

            // Validatie: een gekozen optie moet bij deze reeks horen (null = optie wissen, toegestaan).
            if (request.SelectedPriceOptionId is Guid optionId)
            {
                IReadOnlyList<LessonSeriePrice> options =
                    await priceRepo.GetBySeriesAsync(lessonSeriesId, organizationId, ct);
                if (options.All(o => o.Id != optionId))
                    return Result<LessonSerieEnrollmentDto>.Fail(new Error(ErrorCodes.Validation,
                        "Geselecteerde prijsoptie hoort niet bij deze lessenreeks."));
            }

            // Propagatie: groep → alle leden; solo → enkel deze inschrijving.
            if (enrollment.EnrollmentGroupId is not null)
            {
                Enrollment? withGroup =
                    await enrollmentRepo.GetByIdWithGroupAsync(enrollmentId, organizationId, ct);
                List<Enrollment> members =
                    withGroup?.EnrollmentGroup?.Members.ToList() ?? [enrollment];
                foreach (Enrollment member in members)
                {
                    member.SelectedPriceOptionId = request.SelectedPriceOptionId;
                    member.UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;
                }
            }
            else
            {
                enrollment.SelectedPriceOptionId = request.SelectedPriceOptionId;
            }
        }

        await unitOfWork.SaveChangesAsync(ct);

        LessonSerieEnrollmentDto dto = new()
        {
            Id = enrollment.Id,
            StudentName = enrollment.StudentName,
            StudentEmail = enrollment.StudentEmail,
            StudentPhone = enrollment.StudentPhone,
            ContactEmail = enrollment.ContactEmail,
            HasOwnEmail = enrollment.StudentEmail is not null,
            Status = enrollment.Status.ToString(),
            EnrolledAt = enrollment.EnrolledAt,
            Notes = enrollment.Notes,
            DateOfBirth = enrollment.DateOfBirth?.ToString("yyyy-MM-dd"),
            Category = enrollment.Category.HasValue ? (int)enrollment.Category.Value : null,
            CategoryLabel = enrollment.Category switch
            {
                ParticipantCategory.Youth => "Jeugd",
                ParticipantCategory.Adult => "Volwassenen",
                _ => null,
            },
            EnrollmentGroupId = enrollment.EnrollmentGroupId,
            MaxGroupSize = enrollment.MaxGroupSize,
            SelectedPriceOptionId = enrollment.SelectedPriceOptionId,
        };

        logger.LogInformation(
            "Basisgegevens van inschrijving {EnrollmentId} aangepast door beheerder in organisatie {OrganizationId}",
            enrollmentId, organizationId);

        return Result<LessonSerieEnrollmentDto>.Ok(dto);
    }

    /// <summary>
    /// Past het maximum aantal personen per tijdslot aan (bij een groep voor alle leden). Null of een
    /// ongewijzigde waarde doet niets. Het maximum moet binnen de grenzen van de reeks en de eigen groepsgrootte
    /// vallen, en mag niet lager worden dan het tijdslot waarin de inschrijving nu al ingepland staat: de admin
    /// haalt ze dan eerst uit dat tijdslot.
    /// </summary>
    private async Task<Error?> ApplyMaxGroupSizeAsync(
        Enrollment enrollment, int? requested, Guid lessonSeriesId, Guid organizationId, CancellationToken ct)
    {
        if (requested is null || requested == enrollment.MaxGroupSize)
            return null;

        List<Enrollment> members = [enrollment];
        if (enrollment.EnrollmentGroupId is not null)
        {
            Enrollment? withGroup = await enrollmentRepo.GetByIdWithGroupAsync(enrollment.Id, organizationId, ct);
            if (withGroup?.EnrollmentGroup is not null)
                members = withGroup.EnrollmentGroup.Members.ToList();
        }

        int ownSize = members.Count(m =>
            m.Status is EnrollmentStatus.Confirmed or EnrollmentStatus.Pending or EnrollmentStatus.PendingPayment);
        ownSize = Math.Max(ownSize, 1);

        Domain.Entities.LessonSerie? series = await lessonSeriesRepo.GetByIdAsync(lessonSeriesId, organizationId, ct);
        int seriesMax = GroupSizeRules.SeriesMax((series?.WeeklyTemplate ?? []).Select(w => w.MaxStudents));
        Error? boundsError = GroupSizeRules.Validate(requested.Value, ownSize, seriesMax);
        if (boundsError is not null)
            return boundsError;

        // Verlagen onder de grootte van het tijdslot waarin ze nu al staat, is niet toegestaan.
        HashSet<Guid> memberIds = members.Select(m => m.Id).ToHashSet();
        Guid? groupId = enrollment.EnrollmentGroupId;
        IReadOnlyList<ScheduleAssignment> assignments =
            await scheduleAssignmentRepo.GetBySeriesAsync(lessonSeriesId, organizationId, ct);
        foreach (ScheduleAssignment own in assignments.Where(a =>
                     a.Status != ScheduleAssignmentStatus.Declined
                     && ((a.EnrollmentId.HasValue && memberIds.Contains(a.EnrollmentId.Value))
                         || (groupId.HasValue && a.EnrollmentGroupId == groupId))))
        {
            int slotSize = assignments
                .Where(a => a.WeeklyTemplateEntryId == own.WeeklyTemplateEntryId)
                .Sum(PlanningProposalBuilder.GetEffectiveAssignmentSize);
            if (slotSize > requested.Value)
                return new Error(ErrorCodes.Validation,
                    $"Het maximum kan niet verlaagd worden naar {requested.Value}: deze inschrijving staat in een tijdslot " +
                    $"met {slotSize} personen. Haal ze eerst uit dat tijdslot.");
        }

        foreach (Enrollment member in members)
        {
            member.MaxGroupSize = requested.Value;
            member.UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;
        }

        return null;
    }

    /// <summary>
    /// Herkent een PostgreSQL unique violation (SQLSTATE 23505) ergens in de exception-keten.
    /// Bewust via reflectie: de Application-laag kent Npgsql noch EF Core.
    /// </summary>
    private static bool IsUniqueViolation(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            object? sqlState = current.GetType()
                .GetProperty("SqlState")
                ?.GetValue(current);

            if (sqlState as string == "23505")
                return true;
        }

        return false;
    }

    public async Task<Result<bool>> CancelEnrollmentAsync(
        Guid lessonSeriesId, Guid enrollmentId, Guid organizationId, CancellationToken ct = default)
    {
        // GetByIdAsync filtert op organizationId — dat is meteen de autorisatiecheck.
        Enrollment? enrollment = await enrollmentRepo.GetByIdAsync(enrollmentId, organizationId, ct);
        if (enrollment is null)
            return Result<bool>.Fail(
                new Error(ErrorCodes.NotFound, "Inschrijving niet gevonden."));

        // De inschrijving moet bij de reeks uit de route horen. Zonder deze check zou
        // DELETE /lessonseries/{willekeurige-reeks}/enrollments/{geldige-id} slagen
        // zolang beide in dezelfde organisatie zitten — een cross-serie annulering.
        if (enrollment.LessonSerieId != lessonSeriesId)
            return Result<bool>.Fail(
                new Error(ErrorCodes.NotFound, "Inschrijving niet gevonden."));

        if (enrollment.Status == EnrollmentStatus.Cancelled)
            return Result<bool>.Fail(
                new Error(ErrorCodes.Validation, "Deze inschrijving is al geannuleerd."));

        enrollment.Status = EnrollmentStatus.Cancelled;
        enrollment.UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Inschrijving {EnrollmentId} geannuleerd door beheerder in organisatie {OrganizationId}",
            enrollmentId, organizationId);

        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> CancelGroupAsync(
        Guid lessonSeriesId, Guid groupId, Guid organizationId, CancellationToken ct = default)
    {
        // GetByIdAsync filtert op organizationId (autorisatiecheck) en laadt de leden mee.
        EnrollmentGroup? group = await enrollmentGroupRepo.GetByIdAsync(groupId, organizationId, ct);
        if (group is null)
            return Result<bool>.Fail(new Error(ErrorCodes.NotFound, "Groep niet gevonden."));

        // De groep moet bij de reeks uit de route horen (cross-serie guard).
        if (group.LessonSerieId != lessonSeriesId)
            return Result<bool>.Fail(new Error(ErrorCodes.NotFound, "Groep niet gevonden."));

        List<Enrollment> active = group.Members
            .Where(m => m.Status != EnrollmentStatus.Cancelled)
            .ToList();

        if (active.Count == 0)
            return Result<bool>.Fail(new Error(ErrorCodes.Validation, "Deze groep is al geannuleerd."));

        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        foreach (Enrollment member in active)
        {
            member.Status = EnrollmentStatus.Cancelled;
            member.UpdatedAt = now;
        }

        // Eén SaveChanges = één impliciete transactie: alles-of-niets. De leden zijn
        // getrackt door dezelfde DbContext (GetByIdAsync include't ze zonder AsNoTracking).
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Groep {GroupId} ({Count} leden) geannuleerd door beheerder in organisatie {OrganizationId}",
            groupId, active.Count, organizationId);

        return Result<bool>.Ok(true);
    }

    /// <summary>
    /// Parseert de geboortedatum uit het request. De validator heeft het formaat al
    /// afgedwongen; faalt het hier toch, dan slaan we null op in plaats van te crashen —
    /// de inschrijving zelf mag hier niet op stuklopen.
    /// </summary>
    private static DateOnly? ParseBirthDate(string? value)
        => DateOfBirthRules.TryParse(value, out DateOnly date) ? date : null;

    /// <summary>
    /// Controleert het minimum aantal beschikbare tijdvakken van de reeks. Enkel unieke weekslots van
    /// deze reeks met voorkeur Beschikbaar of Voorkeur tellen mee; Niet beschikbaar telt niet mee.
    /// Het vereiste aantal wordt begrensd door het aantal weekslots (zonder weekslots geen eis).
    /// Alleen voor de publieke inschrijving; handmatige inschrijvingen door de admin zijn vrijgesteld.
    /// </summary>
    private static Error? CheckMinimumAvailableSlots(
        SubmitEnrollmentRequest request, Domain.Entities.LessonSerie series)
    {
        if (series.WeeklyTemplate.Count == 0)
            return null;

        int required = Math.Min(series.MinimumAvailableSlots, series.WeeklyTemplate.Count);

        HashSet<Guid> seriesSlotIds = series.WeeklyTemplate.Select(w => w.Id).ToHashSet();
        int available = (request.TimeSlotPreferences ?? [])
            .Where(p => (p.Preference == (int)SlotPreference.Available || p.Preference == (int)SlotPreference.Preferred)
                        && seriesSlotIds.Contains(p.WeeklyTemplateEntryId))
            .Select(p => p.WeeklyTemplateEntryId)
            .Distinct()
            .Count();

        return available >= required
            ? null
            : new Error(ErrorCodes.Validation,
                $"Kies minstens {required} tijdvak{(required == 1 ? "" : "ken")} waarop je kunt.");
    }

    /// <summary>
    /// Controleert of elke deelnemer (leider + groepsleden) op de startdatum van de reeks
    /// binnen [MinAge, MaxAge] valt. Zonder bruikbare geboortedatum wordt niet geblokkeerd,
    /// consistent met de tariefcategorie en de partiële unique index.
    /// </summary>
    private static Error? CheckAgeEligibility(
        SubmitEnrollmentRequest request, Domain.Entities.LessonSerie series)
    {
        List<(string Name, string? Dob)> people = [(request.StudentName, request.DateOfBirth)];
        if (request.EnrollmentType == "group" && request.GroupMembers is not null)
            people.AddRange(request.GroupMembers.Select(m => (m.StudentName, (string?)m.DateOfBirth)));

        foreach ((string name, string? dob) in people)
        {
            if (!DateOfBirthRules.TryParse(dob, out DateOnly parsed)) continue;

            int age = ParticipantCategoryResolver.CalculateAge(parsed, series.StartDate);
            if (age < series.MinAge || age > series.MaxAge)
            {
                return new Error(ErrorCodes.Validation,
                    $"{name} ({age} jaar) valt buiten de leeftijdsgrens van deze reeks " +
                    $"({series.MinAge}–{series.MaxAge} jaar).");
            }
        }

        return null;
    }

    /// <summary>
    /// Leidt de tariefcategorie af. Zonder bruikbare geboortedatum blijft de categorie
    /// null; <c>PricingService</c> behandelt dat als volwassene.
    /// </summary>
    private static ParticipantCategory? ResolveCategory(
        string? dateOfBirth, int youthMaxAge, DateOnly onDate)
    {
        if (!DateOfBirthRules.TryParse(dateOfBirth, out DateOnly date)) return null;
        return ParticipantCategoryResolver.Resolve(date, youthMaxAge, onDate);
    }

    private static string BuildGroupName(int index)
    {
        // 0 → A, 25 → Z, 26 → AA, 27 → AB, …, 701 → ZZ, 702 → AAA, enz.
        var name = string.Empty;
        var n = index;
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
