using CoachOS.Application.LessonSerie.DTOs;
using CoachOS.Application.Mappings;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;

namespace CoachOS.Application.LessonSerie;

public class LessonSerieService(
    ILessonSerieRepository lessonSeriesRepo,
    ILessonRepository lessonRepo,
    ITennisClubRepository tennisClubRepo,
    IUserLookupService userLookup,
    ApplicationMapper mapper) : ILessonSerieService
{
    public async Task<Result<List<LessonSerieDto>>> GetAllAsync(
        Guid organizationId, Guid? trainerId = null, CancellationToken ct = default)
    {
        var seriesList =
            await lessonSeriesRepo.GetByOrganizationAsync(organizationId, trainerId, ct);

        if (seriesList.Count == 0)
            return Result<List<LessonSerieDto>>.Ok([]);

        var lessonCounts =
            await lessonRepo.GetLessonCountsBySeriesIdsAsync(seriesList.Select(s => s.Id), ct);

        var dtos = seriesList.Select(ls =>
            mapper.ToLessonSerieDto(ls,
                lessonCounts.GetValueOrDefault(ls.Id, 0))
        ).ToList();

        return Result<List<LessonSerieDto>>.Ok(dtos);
    }

    public async Task<Result<LessonSerieDto>> GetByIdAsync(
        Guid id, Guid organizationId, CancellationToken ct = default)
    {
        var series =
            await lessonSeriesRepo.GetByIdAsync(id, organizationId, ct);

        if (series is null)
            return Result<LessonSerieDto>.Fail(new Error(ErrorCodes.NotFound, "LessonSerie niet gevonden."));

        var lessons = series.Lessons
            .OrderBy(l => l.Date)
            .ThenBy(l => l.StartTime)
            .Select(l => mapper.ToLessonDto(l, series.Id))
            .ToList();

        var dto = mapper.ToLessonSerieDto(series, lessons.Count);
        dto.Lessons = lessons;

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

        var series = mapper.ToLessonSerie(request, organizationId);

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
        series.RegistrationDeadline = request.RegistrationDeadline;
        series.IsActive = request.IsActive;
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

        var lesson = mapper.ToLesson(request, series);
        await lessonRepo.AddAsync(lesson, ct);
        await lessonRepo.SaveChangesAsync(ct);

        return Result<Guid>.Ok(lesson.Id);
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
