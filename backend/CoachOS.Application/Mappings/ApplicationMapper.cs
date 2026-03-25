using CoachOS.Application.LessonSeries.DTOs;
using CoachOS.Application.TennisClubs.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;

namespace CoachOS.Application.Mappings;

public class ApplicationMapper
{
    public Domain.Entities.LessonSeries ToLessonSeries(CreateLessonSeriesRequest request, Guid organizationId)
    {
        return new Domain.Entities.LessonSeries
        {
            OrganizationId = organizationId,
            TrainerId = request.TrainerId,
            Name = request.Name,
            Description = request.Description,
            Level = (LessonLevel)request.Level,
            Price = request.Price,
            StartDate = DateOnly.ParseExact(request.StartDate, "yyyy-MM-dd"),
            EndDate = DateOnly.ParseExact(request.EndDate, "yyyy-MM-dd"),
            DurationMinutes = request.DurationMinutes,
            TennisClubId = request.TennisClubId,
            IsActive = true,
        };
    }

    public LessonSeriesDto ToLessonSeriesDto(Domain.Entities.LessonSeries ls, string trainerName, int lessonCount)
    {
        return new LessonSeriesDto
        {
            Id = ls.Id,
            OrganizationId = ls.OrganizationId,
            TrainerId = ls.TrainerId,
            TrainerName = trainerName,
            Name = ls.Name,
            Description = ls.Description,
            Level = (int)ls.Level,
            Price = ls.Price,
            StartDate = ls.StartDate.ToString("yyyy-MM-dd"),
            EndDate = ls.EndDate.ToString("yyyy-MM-dd"),
            DurationMinutes = ls.DurationMinutes,
            IsActive = ls.IsActive,
            LessonCount = lessonCount,
            CreatedAt = ls.CreatedAt,
            TennisClubId = ls.TennisClubId,
            TennisClubName = ls.TennisClub?.Name ?? string.Empty,
            TennisClubAddress = ls.TennisClub?.Address ?? string.Empty,
        };
    }

    public LessonDto ToLessonDto(Lesson lesson, Guid seriesId)
    {
        return new LessonDto
        {
            Id = lesson.Id,
            LessonSeriesId = seriesId,
            Date = lesson.Date.ToString("yyyy-MM-dd"),
            StartTime = lesson.StartTime.ToString("HH:mm"),
            EndTime = lesson.EndTime.ToString("HH:mm"),
            CourtName = lesson.CourtName,
            MaxStudents = lesson.MaxStudents,
            Notes = lesson.Notes,
            IsCancelled = lesson.IsCancelled,
        };
    }

    public Lesson ToLesson(CreateLessonRequest request, Domain.Entities.LessonSeries series)
    {
        DateOnly date = DateOnly.ParseExact(request.Date, "yyyy-MM-dd");
        TimeOnly startTime = TimeOnly.ParseExact(request.StartTime, "HH:mm");
        TimeOnly endTime = startTime.AddMinutes(series.DurationMinutes);

        return new Lesson
        {
            OrganizationId = series.OrganizationId,
            LessonSeriesId = series.Id,
            TrainerId = series.TrainerId,
            CourtName = request.CourtName,
            Date = date,
            StartTime = startTime,
            EndTime = endTime,
            Level = series.Level,
            MaxStudents = 0,
            Notes = request.Notes,
            IsCancelled = false,
        };
    }

    public TennisClubDto ToTennisClubDto(TennisClub club)
    {
        return new TennisClubDto
        {
            Id = club.Id,
            Name = club.Name,
            Address = club.Address,
        };
    }
}
