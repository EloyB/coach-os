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
    ApplicationMapper mapper) : ILessonSerieService
{
    public async Task<Result<List<LessonSerieDto>>> GetAllAsync(
        Guid organizationId, Guid? trainerId = null, CancellationToken ct = default)
    {
        IReadOnlyList<Domain.Entities.LessonSerie> seriesList =
            await lessonSeriesRepo.GetByOrganizationAsync(organizationId, trainerId, ct);

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

        var trainerIds = request.WeeklyTemplate.Select(t => t.TrainerId)
            .Concat(request.Lessons.Select(l => l.TrainerId));
        var trainerError = await ValidateTrainerIdsAsync(trainerIds, organizationId, ct);
        if (trainerError is not null)
            return Result<Guid>.Fail(trainerError);

        // Reject duplicate weekly template entries (same day + time + court = same slot)
        List<(int DayOfWeek, string StartTime, string EndTime, string CourtName)> duplicates = request.WeeklyTemplate
            .GroupBy(t => (t.DayOfWeek, t.StartTime, t.EndTime, CourtName: t.CourtName ?? ""))
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicates.Count > 0)
        {
            string[] dayNames = ["ma", "di", "wo", "do", "vr", "za", "zo"];
            string slots = string.Join(", ", duplicates.Select(d =>
                $"{dayNames[d.DayOfWeek]} {d.StartTime}–{d.EndTime}" + (d.CourtName != "" ? $" ({d.CourtName})" : "")));
            return Result<Guid>.Fail(new Error(ErrorCodes.Validation,
                $"Dubbele tijdsloten in weekindeling: {slots}. Verwijder de duplicaten."));
        }

        Domain.Entities.LessonSerie series = mapper.ToLessonSerie(request, organizationId);

        foreach (WeeklyTemplateEntryRequest templateRequest in request.WeeklyTemplate)
        {
            Domain.Entities.WeeklyTemplateEntry entry = mapper.ToWeeklyTemplateEntry(templateRequest, series);
            series.WeeklyTemplate.Add(entry);
        }

        foreach (var lessonRequest in request.Lessons)
        {
            var lesson = mapper.ToLesson(lessonRequest, series);
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

        series.Name = request.Name;
        series.Description = request.Description;
        series.Level = request.Level.HasValue ? (LessonLevel)request.Level.Value : null;
        series.Price = request.Price;
        series.RegistrationDeadline = DateTime.SpecifyKind(request.RegistrationDeadline, DateTimeKind.Utc);
        series.IsActive = request.IsActive;
        series.MaxRegistrations = request.MaxRegistrations;
        series.TennisClubId = request.TennisClubId;

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

        if (request.TrainerId.HasValue)
        {
            bool isValid = await userLookup.IsActiveTrainerAsync(request.TrainerId.Value, organizationId, ct);
            if (!isValid)
                return Result<Guid>.Fail(
                    new Error(ErrorCodes.Validation, "Deze trainer behoort niet tot deze organisatie."));

            DateOnly lessonDate = DateOnly.ParseExact(request.Date, "yyyy-MM-dd");
            TimeOnly lessonStart = TimeOnly.ParseExact(request.StartTime, "HH:mm");
            TimeOnly lessonEnd = TimeOnly.ParseExact(request.EndTime, "HH:mm");
            Error? conflictError = await CheckTrainerConflictAsync(
                request.TrainerId.Value, lessonDate, lessonStart, lessonEnd, ct: ct);
            if (conflictError is not null)
                return Result<Guid>.Fail(conflictError);
        }

        Domain.Entities.Lesson lesson = mapper.ToLesson(request, series);
        await lessonRepo.AddAsync(lesson, ct);
        await lessonRepo.SaveChangesAsync(ct);

        return Result<Guid>.Ok(lesson.Id);
    }

    public async Task<Result<LessonDto>> UpdateLessonAsync(
        Guid seriesId, Guid lessonId, Guid organizationId, UpdateLessonRequest request, CancellationToken ct = default)
    {
        Domain.Entities.Lesson? lesson = await lessonRepo.GetByIdAsync(lessonId, seriesId, organizationId, ct);
        if (lesson is null)
            return Result<LessonDto>.Fail(new Error(ErrorCodes.NotFound, "Lesmoment niet gevonden."));

        Domain.Entities.LessonSerie? series = await lessonSeriesRepo.GetByIdAsync(seriesId, organizationId, ct);
        if (series is null)
            return Result<LessonDto>.Fail(new Error(ErrorCodes.NotFound, "Lesreeks niet gevonden."));

        // Trainer validation
        if (request.TrainerId.HasValue)
        {
            bool isValid = await userLookup.IsActiveTrainerAsync(request.TrainerId.Value, organizationId, ct);
            if (!isValid)
                return Result<LessonDto>.Fail(
                    new Error(ErrorCodes.Validation, "Deze trainer behoort niet tot deze organisatie."));
            lesson.TrainerId = request.TrainerId.Value;
        }

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

        if (request.CourtName is not null)
            lesson.CourtName = request.CourtName;
        if (request.MaxStudents.HasValue)
            lesson.MaxStudents = request.MaxStudents.Value;
        if (request.Notes is not null)
            lesson.Notes = request.Notes;
        if (request.IsCancelled.HasValue)
            lesson.IsCancelled = request.IsCancelled.Value;

        await lessonRepo.SaveChangesAsync(ct);

        LessonDto dto = mapper.ToLessonDto(lesson, seriesId);
        return Result<LessonDto>.Ok(dto);
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

    public async Task<Result> DeleteLessonAsync(
        Guid seriesId, Guid lessonId, Guid organizationId, CancellationToken ct = default)
    {
        var lesson =
            await lessonRepo.GetByIdWithEnrollmentsAsync(lessonId, seriesId, organizationId, ct);

        if (lesson is null)
            return Result.Fail(new Error(ErrorCodes.NotFound, "Lesmoment niet gevonden."));

        if (lesson.Enrollments.Count > 0)
            return Result.Fail(new Error(ErrorCodes.Conflict, "Verwijderen niet mogelijk: er zijn nog inschrijvingen op dit lesmoment."));

        await lessonRepo.DeleteAsync(lesson, ct);
        await lessonRepo.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
