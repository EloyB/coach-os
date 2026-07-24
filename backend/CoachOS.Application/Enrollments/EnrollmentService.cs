using System.Data;
using System.Text.Json;
using CoachOS.Application.Common;
using CoachOS.Application.Enrollments.DTOs;
using CoachOS.Application.LessonSerie.DTOs;
using CoachOS.Application.Mappings;
using CoachOS.Domain.Common;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using Microsoft.Extensions.Logging;

// Voorkomt ambiguity met de gelijknamige namespace CoachOS.Application.OrganizationSettings.
using OrganizationSettingsEntity = CoachOS.Domain.Entities.OrganizationSettings;

namespace CoachOS.Application.Enrollments;

public class EnrollmentService(
    IEnrollmentRepository enrollmentRepo,
    IEnrollmentFormRepository enrollmentFormRepo,
    ILessonSerieRepository lessonSeriesRepo,
    IEnrollmentGroupRepository enrollmentGroupRepo,
    ITimeSlotPreferenceRepository timeSlotPreferenceRepo,
    IOrganizationSettingsRepository orgSettingsRepo,
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
            MinAge = series.MinAge,
            MaxAge = series.MaxAge,
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

        // 3. Load enrollment form with fields (may be null)
        var form = await enrollmentFormRepo.GetBySeriesIdReadOnlyAsync(lessonSeriesId, ct);

        // 4. Validate form responses against actual form fields (pre-transaction: cheap, no race risk)
        if (form is not null)
        {
            Error? formError = FormResponseValidator.Validate(
                form.Fields.Select(f => (f.Id, f.IsRequired, f.Label)),
                request.Responses.Select(r => (r.FormFieldId, r.Value)));
            if (formError is not null)
                return Result<Guid>.Fail(formError);
        }

        var groupSize = request.EnrollmentType == "group" && request.GroupMembers is not null
            ? request.GroupMembers.Count + 1
            : 1;

        // Leeftijdsgrens: toets elke deelnemer op de startdatum van de reeks. Fail-fast
        // vóór de transactie — geen DB-werk als iemand buiten de grens valt.
        Error? ageError = CheckAgeEligibility(request, series);
        if (ageError is not null)
            return Result<Guid>.Fail(ageError);

        // 4b. Tariefcategorie afleiden. De leeftijdsgrens is org-specifiek; deze flow is
        //     anoniem, dus de settings komen via series.OrganizationId en niet uit een JWT.
        //     De categorie wordt één keer vastgelegd bij inschrijving: wie tijdens de reeks
        //     jarig is, mag niet halverwege van tarief wisselen.
        OrganizationSettingsEntity? orgSettings =
            await orgSettingsRepo.GetByOrganizationReadOnlyAsync(series.OrganizationId, ct);
        int youthMaxAge = orgSettings?.YouthMaxAge ?? 17;
        DateOnly enrolledOn = DateOnly.FromDateTime(DateTime.UtcNow);

        // 5. Begin SERIALIZABLE transaction: capacity + duplicate checks moeten ATOMIC zijn
        //    met de insert, anders kunnen twee parallelle submitters beide de check passeren
        //    en samen MaxRegistrations overschrijden of een duplicate email creëren.
        Enrollment enrollment;
        try
        {
            await enrollmentRepo.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            // 6. Capacity check INSIDE transaction (accounts for group size)
            if (series.MaxRegistrations.HasValue)
            {
                var activeCount = await enrollmentRepo.CountActiveBySeriesAsync(lessonSeriesId, ct);
                if (activeCount + groupSize > series.MaxRegistrations.Value)
                {
                    await enrollmentRepo.RollbackTransactionAsync(ct);
                    return Result<Guid>.Fail(
                        new Error(ErrorCodes.Conflict, "Deze lessenreeks is volzet."));
                }
            }

            // 7. Dubbelcheck INSIDE transaction op persoon, niet op adres: één contactadres
            //    mag meerdere deelnemers dragen (ouder met kinderen, vriend die alles regelt),
            //    maar dezelfde persoon mag niet twee keer in de reeks staan. De partiële unique
            //    index IX_Enrollments_Participant dekt hetzelfde; dit vangt het als een nette
            //    409 vóór de insert i.p.v. een 23505 halverwege de transactie.
            List<(string ContactEmail, string Name, DateOnly? Dob)> participants =
            [
                (EnrollmentEmails.ResolveContactEmail(request, null),
                 request.StudentName, ParseBirthDate(request.DateOfBirth)),
            ];

            if (request.EnrollmentType == "group" && request.GroupMembers is not null)
            {
                participants.AddRange(request.GroupMembers.Select(m =>
                    (EnrollmentEmails.ResolveContactEmail(request, m),
                     m.StudentName, ParseBirthDate(m.DateOfBirth))));
            }

            foreach ((string contactEmail, string name, DateOnly? dob) in participants)
            {
                if (!await enrollmentRepo.IsDuplicateParticipantAsync(
                        lessonSeriesId, contactEmail, name, dob, ct))
                    continue;

                await enrollmentRepo.RollbackTransactionAsync(ct);
                return Result<Guid>.Fail(new Error(
                    ErrorCodes.Conflict, $"{name} is al ingeschreven voor deze lessenreeks."));
            }

        // 8. Create enrollment. Status blijft Pending tot de student via de
        //    confirmation-mail bevestigt — Mollie payment wordt dan getriggerd
        //    door StudentConfirmationService, niet hier.
        enrollment = new()
        {
            OrganizationId = series.OrganizationId,
            LessonSerieId = series.Id,
            StudentName = request.StudentName,
            ContactEmail = EnrollmentEmails.ResolveContactEmail(request, null),
            StudentEmail = EnrollmentEmails.Normalize(request.StudentEmail),
            StudentPhone = request.StudentPhone,
            Status = EnrollmentStatus.Pending,
            EnrolledAt = DateTime.UtcNow,
            IsOpenToGrouping = request.IsOpenToGrouping,
            DateOfBirth = ParseBirthDate(request.DateOfBirth),
            Category = ResolveCategory(request.DateOfBirth, youthMaxAge, enrolledOn),
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
            // Naming: A–Z voor eerste 26 groepen, dan AA, AB, …, BA, BB, … zodat de namen
            // leesbaar blijven (geen non-printables zoals [ of \ bij 'A'+26).
            var groupName = BuildGroupName(existingGroupCount);

            EnrollmentGroup group = new()
            {
                OrganizationId = series.OrganizationId,
                LessonSerieId = series.Id,
                Name = $"Groep {groupName}",
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
                    ContactEmail = EnrollmentEmails.ResolveContactEmail(request, member),
                    StudentEmail = string.IsNullOrWhiteSpace(member.StudentEmail)
                        ? null
                        : EnrollmentEmails.Normalize(member.StudentEmail),
                    StudentPhone = member.StudentPhone,
                    Status = EnrollmentStatus.Pending,
                    EnrolledAt = DateTime.UtcNow,
                    EnrollmentGroupId = group.Id,
                    DateOfBirth = ParseBirthDate(member.DateOfBirth),
                    Category = ResolveCategory(member.DateOfBirth, youthMaxAge, enrolledOn),
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

            // Een unique violation betekent dat een parallelle submitter ons voor was tussen
            // de check en de insert. Dat is een conflict (409), geen serverfout — retryen met
            // dezelfde gegevens gaat nooit lukken, dus zeg dat ook.
            if (IsUniqueViolation(ex))
            {
                logger.LogWarning(ex, "Dubbele inschrijving voor reeks {SeriesId}", lessonSeriesId);
                return Result<Guid>.Fail(new Error(
                    ErrorCodes.Conflict, "Dit e-mailadres is al ingeschreven voor deze lessenreeks."));
            }

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

            // Eén mail per contactadres: wie de communicatie voor meerdere deelnemers
            // draagt, hoort niet drie keer dezelfde bevestiging te krijgen.
            List<(string Email, string Name)> confirmationRecipients =
                [(EnrollmentEmails.ResolveContactEmail(request, null), request.StudentName)];

            if (request.EnrollmentType == "group" && request.GroupMembers is { Count: > 0 })
            {
                confirmationRecipients.AddRange(request.GroupMembers.Select(m =>
                    (EnrollmentEmails.ResolveContactEmail(request, m), m.StudentName)));
            }

            // Groeperen op contactadres: één mail per adres, met alle deelnemers erin
            // benoemd zodat de contactpersoon ziet wie er onder zijn adres valt.
            foreach (IGrouping<string, (string Email, string Name)> contact in
                     confirmationRecipients.GroupBy(r => r.Email))
            {
                List<string> names = contact.Select(r => r.Name).ToList();
                try
                {
                    await emailService.SendEnrollmentConfirmationAsync(
                        contact.Key, names[0], series.Name,
                        trainerInfo?.FullName ?? string.Empty, names, ct);
                }
                catch (Exception memberEx)
                {
                    logger.LogError(memberEx,
                        "Bevestigingsmail naar {Email} mislukt voor inschrijving {EnrollmentId}",
                        contact.Key, enrollment.Id);
                }
            }

            if (trainerInfo.HasValue)
            {
                await emailService.SendEnrollmentNotificationToTrainerAsync(
                    trainerInfo.Value.Email,
                    trainerInfo.Value.FullName,
                    request.StudentName,
                    EnrollmentEmails.ResolveContactEmail(request, null),
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
        enrollment.UpdatedAt = DateTime.UtcNow;
        await enrollmentRepo.SaveChangesAsync(ct);

        logger.LogInformation(
            "Inschrijving {EnrollmentId} geannuleerd door beheerder in organisatie {OrganizationId}",
            enrollmentId, organizationId);

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
