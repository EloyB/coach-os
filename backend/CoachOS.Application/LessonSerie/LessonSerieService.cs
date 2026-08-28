using CoachOS.Application.LessonSerie.DTOs;
using CoachOS.Application.Mappings;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;

namespace CoachOS.Application.LessonSerie;

public class LessonSerieService(
    ILessonSerieRepository lessonSeriesRepo,
    ILessonRepository lessonRepo,
    IEnrollmentRepository enrollmentRepo,
    ITennisClubRepository tennisClubRepo,
    IUserLookupService userLookup,
    IEmailService emailService,
    IMollieConnectionRepository mollieConnectionRepo,
    IScheduleAssignmentRepository scheduleAssignmentRepo,
    ITimeSlotPreferenceRepository timeSlotPreferenceRepo,
    ILessonInvitationRepository lessonInvitationRepo,
    ApplicationMapper mapper) : ILessonSerieService
{
    public async Task<Result<List<LessonSerieDto>>> GetAllAsync(
        Guid organizationId, Guid? trainerId, IReadOnlyList<Guid> headTrainerClubIds, CancellationToken ct = default)
    {
        IReadOnlyList<Domain.Entities.LessonSerie> seriesList =
            await lessonSeriesRepo.GetByOrganizationAsync(organizationId, trainerId, headTrainerClubIds, ct);

        if (seriesList.Count == 0)
            return Result<List<LessonSerieDto>>.Ok([]);

        IEnumerable<Guid> seriesIds = seriesList.Select(s => s.Id);

        Dictionary<Guid, int> lessonCounts =
            await lessonRepo.GetLessonCountsBySeriesIdsAsync(seriesIds, ct);

        Dictionary<Guid, int> enrollmentCounts =
            await enrollmentRepo.CountActiveBySeriesIdsAsync(seriesIds, ct);

        List<LessonSerieDto> dtos = seriesList.Select(ls =>
            mapper.ToLessonSerieDto(ls,
                lessonCounts.GetValueOrDefault(ls.Id, 0),
                enrollmentCounts.GetValueOrDefault(ls.Id, 0))
        ).ToList();

        return Result<List<LessonSerieDto>>.Ok(dtos);
    }

    public async Task<Result<LessonSerieDto>> GetByIdAsync(
        Guid id, Guid organizationId, CancellationToken ct = default)
    {
        Domain.Entities.LessonSerie? series =
            await lessonSeriesRepo.GetByIdAsync(id, organizationId, ct);

        if (series is null)
            return Result<LessonSerieDto>.Fail(new Error(ErrorCodes.NotFound, "LessonSerie niet gevonden."));

        List<LessonDto> lessons = series.Lessons
            .OrderBy(l => l.Date)
            .ThenBy(l => l.StartTime)
            .Select(l => mapper.ToLessonDto(l, series.Id))
            .ToList();

        int enrolledCount = await enrollmentRepo.CountActiveBySeriesAsync(series.Id, ct);

        LessonSerieDto dto = mapper.ToLessonSerieDto(series, lessons.Count, enrolledCount);
        dto.Lessons = lessons;
        dto.WeeklyTemplate = series.WeeklyTemplate
            .OrderBy(w => w.DayOfWeek)
            .ThenBy(w => w.StartTime)
            .Select(mapper.ToWeeklyTemplateEntryDto)
            .ToList();

        return Result<LessonSerieDto>.Ok(dto);
    }

    public async Task<Result<List<LessonSerieMemberDto>>> GetMembersAsync(
        Guid organizationId, CancellationToken ct = default)
    {
        var members =
            await userLookup.GetOrganizationMembersAsync(organizationId, ct);

        var dtos = members
            .Select(m => new LessonSerieMemberDto { Id = m.Id, FullName = m.FullName })
            .ToList();

        return Result<List<LessonSerieMemberDto>>.Ok(dtos);
    }

    public async Task<Result<Guid>> CreateAsync(
        Guid organizationId, CreateLessonSerieRequest request, CancellationToken ct = default)
    {
        var clubExists = await tennisClubRepo.ExistsAsync(request.TennisClubId, organizationId, ct);
        if (!clubExists)
            return Result<Guid>.Fail(new Error(ErrorCodes.NotFound, "Tennisclub niet gevonden."));

        Error? onlinePaymentError =
            await ValidateOnlinePaymentAsync(organizationId, request.AcceptOnlinePayment, ct);
        if (onlinePaymentError is not null)
            return Result<Guid>.Fail(onlinePaymentError);

        var trainerIds = request.WeeklyTemplate.Select(t => t.TrainerId)
            .Concat(request.Lessons.Select(l => l.TrainerId));
        var trainerError = await ValidateTrainerIdsAsync(trainerIds, organizationId, ct);
        if (trainerError is not null)
            return Result<Guid>.Fail(trainerError);

        // Reject duplicate weekly template entries when a court is known.
        // Multiple unnamed parallel lessons are allowed; the club can assign courts later. The key MUST match the unique index
        // IX_WeeklyTemplateEntries_LessonSerieId_DayOfWeek_StartTime_CourtName (die GEEN EndTime bevat);
        // anders glipt een same-start/different-end collision hier langs en wordt het een rauwe
        // DbUpdateException → HTTP 500 in plaats van deze nette validatiefout.
        static string NormalizeCourt(string? court) =>
            string.IsNullOrWhiteSpace(court) ? "" : court.Trim();

        List<(int DayOfWeek, string StartTime, string CourtName)> duplicateKeys = request.WeeklyTemplate
            .GroupBy(t => (t.DayOfWeek, t.StartTime, CourtName: NormalizeCourt(t.CourtName)))
            .Where(g => g.Key.CourtName != "" && g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateKeys.Count > 0)
        {
            string[] dayNames = ["ma", "di", "wo", "do", "vr", "za", "zo"];
            string SlotLabel((int DayOfWeek, string StartTime, string CourtName) key) =>
                $"{dayNames[key.DayOfWeek]} {key.StartTime}";

            // Echte duplicaat: dezelfde dag + starttijd + baannaam expliciet herhaald.
            string dupSlots = string.Join(", ", duplicateKeys.Select(d => $"{SlotLabel(d)} ({d.CourtName})"));
            return Result<Guid>.Fail(new Error(ErrorCodes.Validation,
                $"Dubbele tijdsloten in weekindeling: {dupSlots}. Verwijder de duplicaten."));
        }

        Domain.Entities.LessonSerie series = mapper.ToLessonSerie(request, organizationId);

        foreach (WeeklyTemplateEntryRequest templateRequest in request.WeeklyTemplate)
        {
            Domain.Entities.WeeklyTemplateEntry entry = mapper.ToWeeklyTemplateEntry(templateRequest, series);
            entry.CourtName = NormalizeCourt(templateRequest.CourtName) is { Length: > 0 } normalizedCourt
                ? normalizedCourt
                : null;
            series.WeeklyTemplate.Add(entry);
        }

        foreach (var lessonRequest in request.Lessons)
        {
            var lesson = mapper.ToLesson(lessonRequest, series);
            lesson.CourtName = NormalizeCourt(lessonRequest.CourtName) is { Length: > 0 } normalizedCourt
                ? normalizedCourt
                : null;

            // Koppel de les aan z'n weekslot (match op dag + starttijd + baan) zodat "pas hele
            // weekslot aan" én de planning-synchronisatie werken. Onze DayOfWeek: 0=maandag,
            // System.DayOfWeek: 0=zondag → (dow + 6) % 7. Geen match (losse les) → null.
            int lessonDow = ((int)lesson.Date.DayOfWeek + 6) % 7;
            lesson.WeeklyTemplateEntry = series.WeeklyTemplate.FirstOrDefault(e =>
                e.DayOfWeek == lessonDow
                && e.StartTime == lesson.StartTime
                && NormalizeCourt(e.CourtName) == NormalizeCourt(lesson.CourtName));

            series.Lessons.Add(lesson);
        }

        await lessonSeriesRepo.AddAsync(series, ct);
        await lessonSeriesRepo.SaveChangesAsync(ct);

        return Result<Guid>.Ok(series.Id);
    }

    public async Task<Result<LessonSerieDto>> UpdateAsync(
        Guid id, Guid organizationId, UpdateLessonSerieRequest request, CancellationToken ct = default)
    {
        var series =
            await lessonSeriesRepo.GetByIdAsync(id, organizationId, ct);

        if (series is null)
            return Result<LessonSerieDto>.Fail(new Error(ErrorCodes.NotFound, "LessonSerie niet gevonden."));

        var clubExists = await tennisClubRepo.ExistsAsync(request.TennisClubId, organizationId, ct);
        if (!clubExists)
            return Result<LessonSerieDto>.Fail(new Error(ErrorCodes.NotFound, "Tennisclub niet gevonden."));

        Error? onlinePaymentError =
            await ValidateOnlinePaymentAsync(organizationId, request.AcceptOnlinePayment, ct);
        if (onlinePaymentError is not null)
            return Result<LessonSerieDto>.Fail(onlinePaymentError);

        series.Name = request.Name;
        series.Description = request.Description;
        series.Level = request.Level.HasValue ? (LessonLevel)request.Level.Value : null;
        series.Price = request.Price;
        series.RegistrationDeadline = DateTime.SpecifyKind(request.RegistrationDeadline, DateTimeKind.Utc);
        series.IsActive = request.IsActive;
        series.MaxRegistrations = request.MaxRegistrations;
        series.MinAge = request.MinAge;
        series.MaxAge = request.MaxAge;
        series.TennisClubId = request.TennisClubId;
        series.AllowSoloEnrollment = request.AllowSoloEnrollment;
        series.AllowGroupEnrollment = request.AllowGroupEnrollment;
        series.AcceptOnlinePayment = request.AcceptOnlinePayment;
        series.AcceptManualPayment = request.AcceptManualPayment;

        await lessonSeriesRepo.UpdateAsync(series, ct);
        await lessonSeriesRepo.SaveChangesAsync(ct);

        var lessonCount = await lessonRepo.CountBySeriesIdAsync(series.Id, ct);

        var club = await tennisClubRepo.GetByIdAsync(series.TennisClubId, organizationId, ct);

        var dto = mapper.ToLessonSerieDto(series, lessonCount);
        dto.TennisClubName = club?.Name ?? string.Empty;
        dto.TennisClubAddress = club?.Address ?? string.Empty;

        return Result<LessonSerieDto>.Ok(dto);
    }

    public async Task<Result> DeleteAsync(
        Guid id, Guid organizationId, CancellationToken ct = default)
    {
        var series =
            await lessonSeriesRepo.GetByIdWithEnrollmentsAsync(id, organizationId, ct);

        if (series is null)
            return Result.Fail(new Error(ErrorCodes.NotFound, "LessonSerie niet gevonden."));

        if (series.Enrollments.Count > 0)
            return Result.Fail(new Error(ErrorCodes.Conflict, "Verwijderen niet mogelijk: er zijn nog inschrijvingen op deze serie."));

        // WeeklyTemplateEntry-rijen staan op DeleteBehavior.Restrict en worden niet automatisch opgeruimd;
        // expliciet mee verwijderen, anders faalt SaveChanges met een FK-violation (HTTP 500).
        await lessonSeriesRepo.DeleteWeeklyTemplateRangeAsync(series.WeeklyTemplate, ct);
        await lessonRepo.DeleteRangeAsync(series.Lessons, ct);
        await lessonSeriesRepo.DeleteAsync(series, ct);
        await lessonSeriesRepo.SaveChangesAsync(ct);

        return Result.Ok();
    }

    public async Task<Result<Guid>> AddLessonAsync(
        Guid seriesId, Guid organizationId, CreateLessonRequest request, CancellationToken ct = default)
    {
        var series =
            await lessonSeriesRepo.GetByIdAsync(seriesId, organizationId, ct);

        if (series is null)
            return Result<Guid>.Fail(new Error(ErrorCodes.NotFound, "LessonSerie niet gevonden."));

        DateOnly lessonDate = DateOnly.ParseExact(request.Date, "yyyy-MM-dd");
        TimeOnly lessonStart = TimeOnly.ParseExact(request.StartTime, "HH:mm");
        TimeOnly lessonEnd = TimeOnly.ParseExact(request.EndTime, "HH:mm");

        if (request.TrainerId.HasValue)
        {
            bool isValid = await userLookup.IsActiveTrainerAsync(request.TrainerId.Value, organizationId, ct);
            if (!isValid)
                return Result<Guid>.Fail(
                    new Error(ErrorCodes.Validation, "Deze trainer behoort niet tot deze organisatie."));

            Error? conflictError = await CheckTrainerConflictAsync(
                request.TrainerId.Value, lessonDate, lessonStart, lessonEnd, ct: ct);
            if (conflictError is not null)
                return Result<Guid>.Fail(conflictError);
        }

        Error? courtConflictError = await CheckCourtConflictAsync(
            organizationId, request.CourtName, lessonDate, lessonStart, lessonEnd, ct: ct);
        if (courtConflictError is not null)
            return Result<Guid>.Fail(courtConflictError);

        Domain.Entities.Lesson lesson = mapper.ToLesson(request, series);
        await lessonRepo.AddAsync(lesson, ct);
        await lessonRepo.SaveChangesAsync(ct);

        return Result<Guid>.Ok(lesson.Id);
    }

    public async Task<Result<Guid>> AddWeeklyTemplateEntryAsync(
        Guid seriesId, Guid organizationId, AddWeeklyTemplateEntryRequest request, CancellationToken ct = default)
    {
        Domain.Entities.LessonSerie? series = await lessonSeriesRepo.GetByIdAsync(seriesId, organizationId, ct);
        if (series is null)
            return Result<Guid>.Fail(new Error(ErrorCodes.NotFound, "LessonSerie niet gevonden."));

        if (request.TrainerId.HasValue)
        {
            bool isValid = await userLookup.IsActiveTrainerAsync(request.TrainerId.Value, organizationId, ct);
            if (!isValid)
                return Result<Guid>.Fail(
                    new Error(ErrorCodes.Validation, "Deze trainer behoort niet tot deze organisatie."));
        }

        TimeOnly startTime = TimeOnly.ParseExact(request.StartTime, "HH:mm");
        TimeOnly endTime = TimeOnly.ParseExact(request.EndTime, "HH:mm");
        string newCourt = NormalizeCourt(request.CourtName);

        // Parallelle weekslots op hetzelfde moment (2 trainers/velden) worden onderscheiden via de baannaam;
        // een botsing met een bestaand weekslot op dezelfde dag+start+baan is een duplicaat.
        bool collides = newCourt != "" && series.WeeklyTemplate.Any(e =>
            e.DayOfWeek == request.DayOfWeek
            && e.StartTime == startTime
            && NormalizeCourt(e.CourtName) == newCourt);

        if (collides)
        {
            string[] dayNames = ["ma", "di", "wo", "do", "vr", "za", "zo"];
            string slot = $"{dayNames[request.DayOfWeek]} {request.StartTime}";
            return newCourt == ""
                ? Result<Guid>.Fail(new Error(ErrorCodes.Validation,
                    $"Er staat al een weekslot op {slot} zonder baannaam. " +
                    "Geef dit weekslot een eigen baannaam om het te onderscheiden."))
                : Result<Guid>.Fail(new Error(ErrorCodes.Conflict,
                    $"Er bestaat al een weekslot op {slot} ({newCourt})."));
        }

        Domain.Entities.WeeklyTemplateEntry entry = new()
        {
            LessonSerieId = series.Id,
            DayOfWeek = request.DayOfWeek,
            StartTime = startTime,
            EndTime = endTime,
            TrainerId = request.TrainerId,
            CourtName = newCourt is { Length: > 0 } ? newCourt : null,
            MaxStudents = request.MaxStudents,
        };
        series.WeeklyTemplate.Add(entry);

        // Expandeer naar concrete lesmomenten vanaf vandaag (of de startdatum als die later valt)
        // tot en met de einddatum van de reeks. Zo verschijnt het weekslot zowel in de planning
        // (uit de weekindeling) als in de lesmomenten-kalender (uit de Lesson-rijen).
        LessonLevel? level = request.Level.HasValue ? (LessonLevel)request.Level.Value : null;
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        DateOnly from = series.StartDate > today ? series.StartDate : today;

        foreach (DateOnly date in WeeklyLessonExpander.MatchingDates(request.DayOfWeek, from, series.EndDate))
        {
            series.Lessons.Add(new Domain.Entities.Lesson
            {
                OrganizationId = series.OrganizationId,
                LessonSerieId = series.Id,
                TrainerId = request.TrainerId,
                CourtName = newCourt is { Length: > 0 } ? newCourt : null,
                Date = date,
                StartTime = startTime,
                EndTime = endTime,
                Level = level,
                MaxStudents = request.MaxStudents,
                IsCancelled = false,
                // Navigatie zetten (niet de Id): EF lost de FK op ongeacht Id-generatietiming.
                WeeklyTemplateEntry = entry,
            });
        }

        await lessonSeriesRepo.SaveChangesAsync(ct);
        return Result<Guid>.Ok(entry.Id);
    }

    private static string NormalizeCourt(string? court) =>
        string.IsNullOrWhiteSpace(court) ? "" : court.Trim();

    public async Task<Result<LessonDto>> UpdateLessonAsync(
        Guid seriesId, Guid lessonId, Guid organizationId, UpdateLessonRequest request, CancellationToken ct = default)
    {
        Domain.Entities.Lesson? lesson = await lessonRepo.GetByIdAsync(lessonId, seriesId, organizationId, ct);
        if (lesson is null)
            return Result<LessonDto>.Fail(new Error(ErrorCodes.NotFound, "Lesmoment niet gevonden."));

        Domain.Entities.LessonSerie? series = await lessonSeriesRepo.GetByIdAsync(seriesId, organizationId, ct);
        if (series is null)
            return Result<LessonDto>.Fail(new Error(ErrorCodes.NotFound, "Lesreeks niet gevonden."));

        // Trainer: validate if assigning, allow null to unassign
        if (request.TrainerId.HasValue)
        {
            bool isValid = await userLookup.IsActiveTrainerAsync(request.TrainerId.Value, organizationId, ct);
            if (!isValid)
                return Result<LessonDto>.Fail(
                    new Error(ErrorCodes.Validation, "Deze trainer behoort niet tot deze organisatie."));
        }
        lesson.TrainerId = request.TrainerId;

        if (request.Date is not null)
        {
            DateOnly newDate = DateOnly.ParseExact(request.Date, "yyyy-MM-dd");
            lesson.Date = newDate;
        }

        // Apply time changes
        if (request.StartTime is not null)
            lesson.StartTime = TimeOnly.ParseExact(request.StartTime, "HH:mm");
        if (request.EndTime is not null)
            lesson.EndTime = TimeOnly.ParseExact(request.EndTime, "HH:mm");

        // Validate end > start (after applying partial updates)
        if (lesson.EndTime <= lesson.StartTime)
            return Result<LessonDto>.Fail(new Error(ErrorCodes.Validation,
                "Eindtijd moet na de starttijd liggen."));

        // Duration: min 15 min, max 4 uur
        TimeSpan duration = lesson.EndTime.ToTimeSpan() - lesson.StartTime.ToTimeSpan();
        if (duration.TotalMinutes < 15)
            return Result<LessonDto>.Fail(new Error(ErrorCodes.Validation,
                "Een lesmoment moet minstens 15 minuten duren."));
        if (duration.TotalHours > 4)
            return Result<LessonDto>.Fail(new Error(ErrorCodes.Validation,
                "Een lesmoment mag maximaal 4 uur duren."));

        // Trainer overlap check (cross-org)
        if (lesson.TrainerId.HasValue)
        {
            Error? conflictError = await CheckTrainerConflictAsync(
                lesson.TrainerId.Value, lesson.Date, lesson.StartTime, lesson.EndTime,
                lesson.Id, ct);
            if (conflictError is not null)
                return Result<LessonDto>.Fail(conflictError);
        }

        // Baanbezetting: check op de effectieve baannaam (request wint, anders de bestaande).
        string? effectiveCourtName = request.CourtName is null
            ? lesson.CourtName
            : NormalizeCourt(request.CourtName) is { Length: > 0 } normalizedCourt
                ? normalizedCourt
                : null;
        Error? courtConflictError = await CheckCourtConflictAsync(
            organizationId, effectiveCourtName, lesson.Date, lesson.StartTime, lesson.EndTime,
            lesson.Id, ct);
        if (courtConflictError is not null)
            return Result<LessonDto>.Fail(courtConflictError);

        if (request.CourtName is not null)
            lesson.CourtName = effectiveCourtName;
        if (request.MaxStudents.HasValue)
            lesson.MaxStudents = request.MaxStudents.Value;
        if (request.Notes is not null)
            lesson.Notes = request.Notes;

        bool newlyCancelled = request.IsCancelled == true && !lesson.IsCancelled;
        if (request.IsCancelled.HasValue)
        {
            lesson.IsCancelled = request.IsCancelled.Value;
            if (request.IsCancelled.Value && request.CancellationReason is not null)
                lesson.CancellationReason = request.CancellationReason;
            else if (!request.IsCancelled.Value)
                lesson.CancellationReason = null;
        }

        // Slot-scope: pas de recurring attributen (tijd, trainer, baan, capaciteit) toe op het
        // hele weekslot — de WeeklyTemplateEntry én alle niet-geannuleerde lessen ervan — zodat de
        // planning (die de template leest) meteen meegaat. Datum en annulering blijven per les.
        // series.WeeklyTemplate en series.Lessons zijn door dezelfde DbContext getrackt als `lesson`,
        // dus deze mutaties gaan mee in één SaveChanges (atomair).
        if (string.Equals(request.ApplyTo, "slot", StringComparison.OrdinalIgnoreCase)
            && lesson.WeeklyTemplateEntryId is Guid templateEntryId)
        {
            Domain.Entities.WeeklyTemplateEntry? entry =
                series.WeeklyTemplate.FirstOrDefault(w => w.Id == templateEntryId);
            if (entry is not null)
            {
                List<Domain.Entities.Lesson> siblings = series.Lessons.Where(l =>
                    l.WeeklyTemplateEntryId == templateEntryId && l.Id != lesson.Id && !l.IsCancelled).ToList();

                // De bewerkte les is hierboven al op conflicten gecheckt; controleer nu ook élke
                // zusterles die dezelfde nieuwe tijd/trainer/baan krijgt, zodat de propagatie geen
                // trainer- of baanconflict verstopt. Nog niets opgeslagen → atomair afbreken bij conflict.
                Error? slotConflict = await CheckSlotConflictsAsync(
                    organizationId, siblings, lesson.TrainerId, lesson.StartTime, lesson.EndTime,
                    lesson.CourtName, ct);
                if (slotConflict is not null)
                    return Result<LessonDto>.Fail(slotConflict);

                entry.StartTime = lesson.StartTime;
                entry.EndTime = lesson.EndTime;
                entry.TrainerId = lesson.TrainerId;
                entry.CourtName = lesson.CourtName;
                entry.MaxStudents = lesson.MaxStudents;

                foreach (Domain.Entities.Lesson sibling in siblings)
                {
                    sibling.StartTime = lesson.StartTime;
                    sibling.EndTime = lesson.EndTime;
                    sibling.TrainerId = lesson.TrainerId;
                    sibling.CourtName = lesson.CourtName;
                    sibling.MaxStudents = lesson.MaxStudents;
                }
            }
        }

        await lessonRepo.SaveChangesAsync(ct);

        if (newlyCancelled && lesson.LessonSerieId.HasValue)
        {
            List<Domain.Entities.Enrollment> enrollments =
                await enrollmentRepo.GetBySeriesAsync(lesson.LessonSerieId.Value, organizationId, ct);

            List<Domain.Entities.Enrollment> activeEnrollments = enrollments
                .Where(e => e.Status is Domain.Enums.EnrollmentStatus.Pending
                    or Domain.Enums.EnrollmentStatus.Confirmed
                    or Domain.Enums.EnrollmentStatus.PendingPayment)
                .ToList();

            // Eén mail per contactadres: een ouder met drie kinderen in de reeks hoort
            // één annuleringsbericht te krijgen, geen drie.
            foreach (Domain.Entities.Enrollment enrollment in activeEnrollments.DistinctBy(e => e.ContactEmail))
            {
                _ = emailService.SendLessonCancellationAsync(
                    enrollment.ContactEmail,
                    enrollment.StudentName,
                    series.Name,
                    lesson.Date,
                    lesson.StartTime,
                    lesson.CancellationReason);
            }
        }

        LessonDto dto = mapper.ToLessonDto(lesson, seriesId);
        return Result<LessonDto>.Ok(dto);
    }

    private async Task<Error?> ValidateOnlinePaymentAsync(
        Guid organizationId, bool acceptOnlinePayment, CancellationToken ct)
    {
        if (!acceptOnlinePayment)
            return null;

        Domain.Entities.MollieConnection? connection =
            await mollieConnectionRepo.GetByOrganizationReadOnlyAsync(organizationId, ct);
        if (connection is null)
            return new Error(ErrorCodes.Validation,
                "Online betalen kan pas aangezet worden nadat de organisatie met Mollie verbonden is.");

        return null;
    }

    private async Task<Error?> ValidateTrainerIdsAsync(
        IEnumerable<Guid?> trainerIds, Guid organizationId, CancellationToken ct)
    {
        var distinctIds = trainerIds
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        foreach (var trainerId in distinctIds)
        {
            var isValid = await userLookup.IsActiveTrainerAsync(trainerId, organizationId, ct);
            if (!isValid)
                return new Error(ErrorCodes.Validation,
                    "Een of meer geselecteerde trainers behoren niet tot deze organisatie.");
        }
        return null;
    }

    private async Task<Error?> CheckTrainerConflictAsync(
        Guid trainerId, DateOnly date, TimeOnly startTime, TimeOnly endTime,
        Guid? excludeLessonId = null, CancellationToken ct = default)
    {
        Domain.Entities.Lesson? conflict = await lessonRepo.FindTrainerConflictAsync(
            trainerId, date, startTime, endTime, excludeLessonId, ct);

        if (conflict is null)
            return null;

        string seriesName = conflict.LessonSerie?.Name ?? "onbekende reeks";
        string conflictTime = $"{conflict.StartTime:HH:mm}–{conflict.EndTime:HH:mm}";
        return new Error(ErrorCodes.Conflict,
            $"Deze trainer heeft al een les op {conflict.Date:dd/MM/yyyy} van {conflictTime} ({seriesName}).");
    }

    private async Task<Error?> CheckCourtConflictAsync(
        Guid organizationId, string? courtName, DateOnly date, TimeOnly startTime, TimeOnly endTime,
        Guid? excludeLessonId = null, CancellationToken ct = default)
    {
        // Geen baan opgegeven → geen bezetting mogelijk.
        if (string.IsNullOrWhiteSpace(courtName))
            return null;

        Domain.Entities.Lesson? conflict = await lessonRepo.FindCourtConflictAsync(
            organizationId, courtName, date, startTime, endTime, excludeLessonId, ct);

        if (conflict is null)
            return null;

        string seriesName = conflict.LessonSerie?.Name ?? "onbekende reeks";
        string conflictTime = $"{conflict.StartTime:HH:mm}–{conflict.EndTime:HH:mm}";
        return new Error(ErrorCodes.Conflict,
            $"{courtName.Trim()} is op {conflict.Date:dd/MM/yyyy} van {conflictTime} al bezet door reeks {seriesName}.");
    }

    /// <summary>
    /// Valideert trainer- en baanconflicten voor de VOLLEDIGE set lessen die door een
    /// slot-wijziging nieuwe tijd/trainer/baan krijgen — niet enkel de bewerkte les. Elke les zit
    /// op een eigen datum (weekritme), dus we checken per datum tegen de rest van de DB (de les
    /// zelf uitgesloten). Zonder deze check kan een slot-wijziging een trainer dubbel boeken of
    /// twee lessen op dezelfde baan zetten zonder dat het opgemerkt wordt.
    /// </summary>
    private async Task<Error?> CheckSlotConflictsAsync(
        Guid organizationId, IEnumerable<Domain.Entities.Lesson> affected,
        Guid? trainerId, TimeOnly startTime, TimeOnly endTime, string? courtName, CancellationToken ct)
    {
        foreach (Domain.Entities.Lesson lesson in affected)
        {
            if (trainerId.HasValue)
            {
                Error? trainerConflict = await CheckTrainerConflictAsync(
                    trainerId.Value, lesson.Date, startTime, endTime, lesson.Id, ct);
                if (trainerConflict is not null)
                    return trainerConflict;
            }

            Error? courtConflict = await CheckCourtConflictAsync(
                organizationId, courtName, lesson.Date, startTime, endTime, lesson.Id, ct);
            if (courtConflict is not null)
                return courtConflict;
        }

        return null;
    }

    public async Task<Result> DeleteLessonAsync(
        Guid seriesId, Guid lessonId, Guid organizationId, bool wholeSlot = false, CancellationToken ct = default)
    {
        var lesson =
            await lessonRepo.GetByIdWithEnrollmentsAsync(lessonId, seriesId, organizationId, ct);

        if (lesson is null)
            return Result.Fail(new Error(ErrorCodes.NotFound, "Lesmoment niet gevonden."));

        // Hele weekslot verwijderen (enkel als de les uit een weekslot komt).
        if (wholeSlot && lesson.WeeklyTemplateEntryId is Guid templateEntryId)
            return await DeleteWeekSlotAsync(seriesId, templateEntryId, organizationId, ct);

        if (lesson.Enrollments.Count > 0)
            return Result.Fail(new Error(ErrorCodes.Conflict, "Verwijderen niet mogelijk: er zijn nog inschrijvingen op dit lesmoment."));

        await lessonRepo.DeleteAsync(lesson, ct);
        await lessonRepo.SaveChangesAsync(ct);

        return Result.Ok();
    }

    /// <summary>
    /// Verwijdert een volledig weekslot over de hele reeks: de <see cref="WeeklyTemplateEntry"/>,
    /// al z'n lessen, z'n beschikbaarheid-voorkeuren en z'n nog-voorgestelde planning-toewijzingen —
    /// in één transactie, zodat het slot ook uit de planning verdwijnt. Blokkeert wanneer het slot
    /// bevestigde of te-bevestigen toewijzingen heeft (geen student verliest stil een bevestigde plaats).
    /// De inschrijvingen zelf zitten op de reeks en blijven bestaan.
    /// </summary>
    public async Task<Result> DeleteWeekSlotAsync(
        Guid seriesId, Guid templateEntryId, Guid organizationId, CancellationToken ct = default)
    {
        Domain.Entities.LessonSerie? series = await lessonSeriesRepo.GetByIdAsync(seriesId, organizationId, ct);
        if (series is null)
            return Result.Fail(new Error(ErrorCodes.NotFound, "Lesreeks niet gevonden."));

        Domain.Entities.WeeklyTemplateEntry? entry =
            series.WeeklyTemplate.FirstOrDefault(w => w.Id == templateEntryId);
        if (entry is null)
            return Result.Fail(new Error(ErrorCodes.NotFound, "Weekslot niet gevonden."));

        List<Domain.Entities.ScheduleAssignment> assignments =
            (await scheduleAssignmentRepo.GetBySeriesAsync(seriesId, organizationId, ct))
            .Where(a => a.WeeklyTemplateEntryId == templateEntryId)
            .ToList();

        if (assignments.Any(a => a.Status is ScheduleAssignmentStatus.Confirmed
                or ScheduleAssignmentStatus.AwaitingConfirmation))
        {
            return Result.Fail(new Error(ErrorCodes.Conflict,
                "Dit weekslot zit al in de planning (bevestigde of nog te bevestigen toewijzingen). " +
                "Maak eerst de planning van dit slot ongedaan voor je het verwijdert."));
        }

        List<Domain.Entities.TimeSlotPreference> preferences =
            (await timeSlotPreferenceRepo.GetBySeriesAsync(seriesId, organizationId, ct))
            .Where(p => p.WeeklyTemplateEntryId == templateEntryId)
            .ToList();

        List<Domain.Entities.Lesson> slotLessons = series.Lessons
            .Where(l => l.WeeklyTemplateEntryId == templateEntryId)
            .ToList();

        // Lessen kunnen rechtstreeks gekoppelde inschrijvingen (Enrollment.LessonId) of uitnodigingen
        // (LessonInvitation.LessonId) hebben; beide FK's staan op Restrict. Zonder deze check zou het
        // verwijderen van de lessen een rauwe FK-schending → HTTP 500 geven. Blokkeer met een nette
        // conflictmelding, net zoals het verwijderen van één los lesmoment doet bij inschrijvingen.
        List<Guid> slotLessonIds = slotLessons.Select(l => l.Id).ToList();
        if (slotLessonIds.Count > 0)
        {
            bool hasEnrollments = await enrollmentRepo.AnyByLessonIdsAsync(slotLessonIds, ct);
            bool hasInvitations = await lessonInvitationRepo.AnyByLessonIdsAsync(slotLessonIds, ct);
            if (hasEnrollments || hasInvitations)
                return Result.Fail(new Error(ErrorCodes.Conflict,
                    "Dit weekslot heeft lessen met gekoppelde inschrijvingen of uitnodigingen. " +
                    "Verwijder of verplaats die eerst voor je het weekslot verwijdert."));
        }

        // Kinderen vóór de ouder, alles in één SaveChanges = één transactie (alles-of-niets).
        // assignments bevatten hier enkel Proposed/Declined (bevestigd is hierboven geblokkeerd),
        // die hebben geen bevestigings-tokens, dus geen verdere keten nodig.
        //
        // assignments/preferences komen uit een AsNoTracking-query mét Include(WeeklyTemplateEntry):
        // die meegeladen entry-instances zouden bij RemoveRange botsen met de al-getrackte entry uit
        // `series` ("cannot be tracked because another instance with the same key is already tracked").
        // Verwijder daarom via key-only stubs — EF hangt ze puur op hun PK aan als Deleted, zonder
        // navigatie-graph. Veilig omdat GetByIdAsync geen assignments/preferences trackt.
        List<Domain.Entities.ScheduleAssignment> assignmentStubs =
            assignments.Select(a => new Domain.Entities.ScheduleAssignment { Id = a.Id }).ToList();
        List<Domain.Entities.TimeSlotPreference> preferenceStubs =
            preferences.Select(p => new Domain.Entities.TimeSlotPreference { Id = p.Id }).ToList();

        scheduleAssignmentRepo.RemoveRange(assignmentStubs);
        timeSlotPreferenceRepo.RemoveRange(preferenceStubs);
        await lessonRepo.DeleteRangeAsync(slotLessons, ct);
        await lessonSeriesRepo.DeleteWeeklyTemplateRangeAsync([entry], ct);
        await lessonSeriesRepo.SaveChangesAsync(ct);

        return Result.Ok();
    }

    public async Task<Result> UpdateWeekSlotAsync(
        Guid seriesId, Guid weeklyTemplateEntryId, Guid organizationId,
        UpdateWeekSlotRequest request, CancellationToken ct = default)
    {
        Domain.Entities.LessonSerie? series = await lessonSeriesRepo.GetByIdAsync(seriesId, organizationId, ct);
        if (series is null)
            return Result.Fail(new Error(ErrorCodes.NotFound, "Lesreeks niet gevonden."));

        Domain.Entities.WeeklyTemplateEntry? entry =
            series.WeeklyTemplate.FirstOrDefault(w => w.Id == weeklyTemplateEntryId);
        if (entry is null)
            return Result.Fail(new Error(ErrorCodes.NotFound, "Weekslot niet gevonden."));

        if (request.TrainerId.HasValue)
        {
            bool isValid = await userLookup.IsActiveTrainerAsync(request.TrainerId.Value, organizationId, ct);
            if (!isValid)
                return Result.Fail(new Error(ErrorCodes.Validation, "Deze trainer behoort niet tot deze organisatie."));
        }

        // Defensief parsen: de validator checkt het HH:mm-formaat, maar een niet-bestaande tijd
        // (bv. "25:00") mag nooit als een rauwe FormatException → HTTP 500 doorlekken.
        if (!TimeOnly.TryParseExact(request.StartTime, "HH:mm", out TimeOnly start)
            || !TimeOnly.TryParseExact(request.EndTime, "HH:mm", out TimeOnly end))
            return Result.Fail(new Error(ErrorCodes.Validation,
                "Ongeldige tijd. Gebruik het formaat HH:mm (00:00–23:59)."));
        if (end <= start)
            return Result.Fail(new Error(ErrorCodes.Validation, "Eindtijd moet na de starttijd liggen."));
        TimeSpan duration = end.ToTimeSpan() - start.ToTimeSpan();
        if (duration.TotalMinutes < 15)
            return Result.Fail(new Error(ErrorCodes.Validation, "Een lesmoment moet minstens 15 minuten duren."));
        if (duration.TotalHours > 4)
            return Result.Fail(new Error(ErrorCodes.Validation, "Een lesmoment mag maximaal 4 uur duren."));

        string? court = NormalizeCourt(request.CourtName) is { Length: > 0 } c ? c : null;

        List<Domain.Entities.Lesson> affected = series.Lessons
            .Where(l => l.WeeklyTemplateEntryId == weeklyTemplateEntryId && !l.IsCancelled)
            .ToList();

        // Valideer trainer/baan-conflicten over de VOLLEDIGE set lessen vóór we iets muteren,
        // anders kan een slot-wijziging stil overlappende lessen voor de trainer of baan maken.
        Error? slotConflict = await CheckSlotConflictsAsync(
            organizationId, affected, request.TrainerId, start, end, court, ct);
        if (slotConflict is not null)
            return Result.Fail(slotConflict);

        // Slot + alle niet-geannuleerde lessen ervan bijwerken → planning gaat mee. Eén SaveChanges.
        entry.StartTime = start;
        entry.EndTime = end;
        entry.TrainerId = request.TrainerId;
        entry.CourtName = court;
        entry.MaxStudents = request.MaxStudents;

        foreach (Domain.Entities.Lesson lesson in affected)
        {
            lesson.StartTime = start;
            lesson.EndTime = end;
            lesson.TrainerId = request.TrainerId;
            lesson.CourtName = court;
            lesson.MaxStudents = request.MaxStudents;
        }

        await lessonSeriesRepo.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result<Guid>> GetClubIdAsync(
        Guid id, Guid organizationId, CancellationToken ct = default)
    {
        Domain.Entities.LessonSerie? series =
            await lessonSeriesRepo.GetByIdAsync(id, organizationId, ct);

        if (series is null)
            return Result<Guid>.Fail(new Error(ErrorCodes.NotFound, "LessonSerie niet gevonden."));

        return Result<Guid>.Ok(series.TennisClubId);
    }
}
