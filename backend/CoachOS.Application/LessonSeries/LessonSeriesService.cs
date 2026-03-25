using CoachOS.Application.LessonSeries.DTOs;
using CoachOS.Application.Mappings;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;

namespace CoachOS.Application.LessonSeries;

public class LessonSeriesService(
    ILessonSeriesRepository lessonSeriesRepo,
    ILessonRepository lessonRepo,
    ITennisClubRepository tennisClubRepo,
    IUserLookupService userLookup,
    ApplicationMapper mapper) : ILessonSeriesService
{
    public async Task<Result<List<LessonSeriesDto>>> GetAllAsync(
        Guid organizationId, Guid? trainerId = null, CancellationToken ct = default)
    {
        IReadOnlyList<Domain.Entities.LessonSeries> seriesList =
            await lessonSeriesRepo.GetByOrganizationAsync(organizationId, trainerId, ct);

        if (seriesList.Count == 0)
            return Result<List<LessonSeriesDto>>.Ok([]);

        List<Guid> trainerIds = seriesList.Select(ls => ls.TrainerId).Distinct().ToList();
        Dictionary<Guid, string> trainerNames = await userLookup.GetUserNamesByIdsAsync(trainerIds, ct);

        Dictionary<Guid, int> lessonCounts = new();
        foreach (Guid seriesId in seriesList.Select(s => s.Id))
        {
            lessonCounts[seriesId] = await lessonRepo.CountBySeriesIdAsync(seriesId, ct);
        }

        List<LessonSeriesDto> dtos = seriesList.Select(ls =>
            mapper.ToLessonSeriesDto(ls,
                trainerNames.GetValueOrDefault(ls.TrainerId, string.Empty),
                lessonCounts.GetValueOrDefault(ls.Id, 0))
        ).ToList();

        return Result<List<LessonSeriesDto>>.Ok(dtos);
    }

    public async Task<Result<LessonSeriesDto>> GetByIdAsync(
        Guid id, Guid organizationId, CancellationToken ct = default)
    {
        Domain.Entities.LessonSeries? series =
            await lessonSeriesRepo.GetByIdAsync(id, organizationId, ct);

        if (series is null)
            return Result<LessonSeriesDto>.Fail(new Error(ErrorCodes.NotFound, "LessonSeries niet gevonden."));

        string trainerName = await userLookup.GetUserNameByIdAsync(series.TrainerId, ct) ?? string.Empty;

        List<LessonDto> lessons = series.Lessons
            .OrderBy(l => l.Date)
            .ThenBy(l => l.StartTime)
            .Select(l => mapper.ToLessonDto(l, series.Id))
            .ToList();

        LessonSeriesDto dto = mapper.ToLessonSeriesDto(series, trainerName, lessons.Count);
        dto.Lessons = lessons;

        return Result<LessonSeriesDto>.Ok(dto);
    }

    public async Task<Result<List<LessonSeriesMemberDto>>> GetMembersAsync(
        Guid organizationId, CancellationToken ct = default)
    {
        List<(Guid Id, string FullName)> members =
            await userLookup.GetOrganizationMembersAsync(organizationId, ct);

        List<LessonSeriesMemberDto> dtos = members
            .Select(m => new LessonSeriesMemberDto { Id = m.Id, FullName = m.FullName })
            .ToList();

        return Result<List<LessonSeriesMemberDto>>.Ok(dtos);
    }

    public async Task<Result<Guid>> CreateAsync(
        Guid organizationId, CreateLessonSeriesRequest request, CancellationToken ct = default)
    {
        bool clubExists = await tennisClubRepo.ExistsAsync(request.TennisClubId, organizationId, ct);
        if (!clubExists)
            return Result<Guid>.Fail(new Error(ErrorCodes.NotFound, "Tennisclub niet gevonden."));

        bool trainerValid = await userLookup.IsActiveTrainerAsync(request.TrainerId, organizationId, ct);
        if (!trainerValid)
            return Result<Guid>.Fail(new Error(ErrorCodes.NotFound, "Trainer niet gevonden of niet actief in deze organisatie."));

        Domain.Entities.LessonSeries series = mapper.ToLessonSeries(request, organizationId);
        await lessonSeriesRepo.AddAsync(series, ct);

        return Result<Guid>.Ok(series.Id);
    }

    public async Task<Result<LessonSeriesDto>> UpdateAsync(
        Guid id, Guid organizationId, UpdateLessonSeriesRequest request, CancellationToken ct = default)
    {
        Domain.Entities.LessonSeries? series =
            await lessonSeriesRepo.GetByIdAsync(id, organizationId, ct);

        if (series is null)
            return Result<LessonSeriesDto>.Fail(new Error(ErrorCodes.NotFound, "LessonSeries niet gevonden."));

        bool clubExists = await tennisClubRepo.ExistsAsync(request.TennisClubId, organizationId, ct);
        if (!clubExists)
            return Result<LessonSeriesDto>.Fail(new Error(ErrorCodes.NotFound, "Tennisclub niet gevonden."));

        series.TrainerId = request.TrainerId;
        series.Name = request.Name;
        series.Description = request.Description;
        series.Level = (LessonLevel)request.Level;
        series.Price = request.Price;
        series.IsActive = request.IsActive;
        series.TennisClubId = request.TennisClubId;

        await lessonSeriesRepo.UpdateAsync(series, ct);

        string trainerName = await userLookup.GetUserNameByIdAsync(series.TrainerId, ct) ?? string.Empty;
        int lessonCount = await lessonRepo.CountBySeriesIdAsync(series.Id, ct);

        Domain.Entities.TennisClub? club = await tennisClubRepo.GetByIdAsync(series.TennisClubId, organizationId, ct);

        LessonSeriesDto dto = mapper.ToLessonSeriesDto(series, trainerName, lessonCount);
        dto.TennisClubName = club?.Name ?? string.Empty;
        dto.TennisClubAddress = club?.Address ?? string.Empty;

        return Result<LessonSeriesDto>.Ok(dto);
    }

    public async Task<Result> DeleteAsync(
        Guid id, Guid organizationId, CancellationToken ct = default)
    {
        Domain.Entities.LessonSeries? series =
            await lessonSeriesRepo.GetByIdWithEnrollmentsAsync(id, organizationId, ct);

        if (series is null)
            return Result.Fail(new Error(ErrorCodes.NotFound, "LessonSeries niet gevonden."));

        if (series.Enrollments.Count > 0)
            return Result.Fail(new Error(ErrorCodes.Conflict, "Verwijderen niet mogelijk: er zijn nog inschrijvingen op deze serie."));

        await lessonRepo.DeleteRangeAsync(series.Lessons, ct);
        await lessonSeriesRepo.DeleteAsync(series, ct);

        return Result.Ok();
    }

    public async Task<Result<Guid>> AddLessonAsync(
        Guid seriesId, Guid organizationId, CreateLessonRequest request, CancellationToken ct = default)
    {
        Domain.Entities.LessonSeries? series =
            await lessonSeriesRepo.GetByIdAsync(seriesId, organizationId, ct);

        if (series is null)
            return Result<Guid>.Fail(new Error(ErrorCodes.NotFound, "LessonSeries niet gevonden."));

        Domain.Entities.Lesson lesson = mapper.ToLesson(request, series);
        await lessonRepo.AddAsync(lesson, ct);

        return Result<Guid>.Ok(lesson.Id);
    }

    public async Task<Result> DeleteLessonAsync(
        Guid seriesId, Guid lessonId, Guid organizationId, CancellationToken ct = default)
    {
        Domain.Entities.Lesson? lesson =
            await lessonRepo.GetByIdWithEnrollmentsAsync(lessonId, seriesId, organizationId, ct);

        if (lesson is null)
            return Result.Fail(new Error(ErrorCodes.NotFound, "Lesmoment niet gevonden."));

        if (lesson.Enrollments.Count > 0)
            return Result.Fail(new Error(ErrorCodes.Conflict, "Verwijderen niet mogelijk: er zijn nog inschrijvingen op dit lesmoment."));

        await lessonRepo.DeleteAsync(lesson, ct);

        return Result.Ok();
    }
}
