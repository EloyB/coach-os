using CoachOS.Application.Enrollments.DTOs;
using CoachOS.Application.LessonSerie.DTOs;
using CoachOS.Application.TennisClubs.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using Riok.Mapperly.Abstractions;

namespace CoachOS.Application.Mappings;

[Mapper]
public partial class ApplicationMapper
{
    public Domain.Entities.LessonSerie ToLessonSerie(CreateLessonSerieRequest request, Guid organizationId)
    {
        return new Domain.Entities.LessonSerie
        {
            OrganizationId = organizationId,
            Name = request.Name,
            Description = request.Description,
            Level = request.Level.HasValue ? (LessonLevel)request.Level.Value : null,
            Price = request.Price,
            StartDate = DateOnly.ParseExact(request.StartDate, "yyyy-MM-dd"),
            EndDate = DateOnly.ParseExact(request.EndDate, "yyyy-MM-dd"),
            RegistrationDeadline = DateTime.SpecifyKind(request.RegistrationDeadline, DateTimeKind.Utc),
            TennisClubId = request.TennisClubId,
            MaxRegistrations = request.MaxRegistrations,
            IsActive = true,
        };
    }

    public LessonSerieDto ToLessonSerieDto(Domain.Entities.LessonSerie ls, int lessonCount, int enrolledCount = 0)
    {
        int totalCapacity = ls.MaxRegistrations ?? 0;
        return new LessonSerieDto
        {
            Id = ls.Id,
            OrganizationId = ls.OrganizationId,
            Name = ls.Name,
            Description = ls.Description,
            Level = ls.Level.HasValue ? (int)ls.Level.Value : null,
            Price = ls.Price,
            StartDate = ls.StartDate.ToString("yyyy-MM-dd"),
            EndDate = ls.EndDate.ToString("yyyy-MM-dd"),
            RegistrationDeadline = ls.RegistrationDeadline,
            IsActive = ls.IsActive,
            MaxRegistrations = ls.MaxRegistrations,
            LessonCount = lessonCount,
            EnrolledCount = enrolledCount,
            TotalCapacity = totalCapacity,
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
            LessonSerieId = seriesId,
            TrainerId = lesson.TrainerId,
            Date = lesson.Date.ToString("yyyy-MM-dd"),
            StartTime = lesson.StartTime.ToString("HH:mm"),
            EndTime = lesson.EndTime.ToString("HH:mm"),
            CourtName = lesson.CourtName,
            Level = lesson.Level.HasValue ? (int)lesson.Level.Value : null,
            MaxStudents = lesson.MaxStudents,
            Notes = lesson.Notes,
            IsCancelled = lesson.IsCancelled,
        };
    }

    public Lesson ToLesson(CreateLessonRequest request, Domain.Entities.LessonSerie series)
    {
        var date = DateOnly.ParseExact(request.Date, "yyyy-MM-dd");
        var startTime = TimeOnly.ParseExact(request.StartTime, "HH:mm");
        var endTime = TimeOnly.ParseExact(request.EndTime, "HH:mm");

        return new Lesson
        {
            OrganizationId = series.OrganizationId,
            LessonSerieId = series.Id,
            TrainerId = request.TrainerId,
            CourtName = request.CourtName,
            Date = date,
            StartTime = startTime,
            EndTime = endTime,
            Level = request.Level.HasValue ? (LessonLevel)request.Level.Value : null,
            MaxStudents = request.MaxStudents,
            Notes = request.Notes,
            IsCancelled = false,
        };
    }

    public WeeklyTemplateEntry ToWeeklyTemplateEntry(WeeklyTemplateEntryRequest request, Domain.Entities.LessonSerie series)
    {
        return new WeeklyTemplateEntry
        {
            LessonSerieId = series.Id,
            DayOfWeek = request.DayOfWeek,
            StartTime = TimeOnly.ParseExact(request.StartTime, "HH:mm"),
            EndTime = TimeOnly.ParseExact(request.EndTime, "HH:mm"),
            TrainerId = request.TrainerId,
            CourtName = request.CourtName,
            MaxStudents = request.MaxStudents,
        };
    }

    public WeeklyTemplateEntryDto ToWeeklyTemplateEntryDto(WeeklyTemplateEntry entry)
    {
        return new WeeklyTemplateEntryDto
        {
            Id = entry.Id,
            DayOfWeek = entry.DayOfWeek,
            StartTime = entry.StartTime.ToString("HH:mm"),
            EndTime = entry.EndTime.ToString("HH:mm"),
            TrainerId = entry.TrainerId,
            CourtName = entry.CourtName,
            MaxStudents = entry.MaxStudents,
        };
    }

    public PublicTimeSlotDto ToPublicTimeSlotDto(WeeklyTemplateEntry entry)
    {
        return new PublicTimeSlotDto
        {
            Id = entry.Id,
            DayOfWeek = entry.DayOfWeek,
            StartTime = entry.StartTime.ToString("HH:mm"),
            EndTime = entry.EndTime.ToString("HH:mm"),
            CourtName = entry.CourtName,
            MaxStudents = entry.MaxStudents,
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
