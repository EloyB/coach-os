using CoachOS.Application.LessonSerie.DTOs;
using CoachOS.Application.Mappings;
using CoachOS.Application.TrainerAvailabilities.DTOs;
using CoachOS.Domain.Entities;
using FluentAssertions;
using NUnit.Framework;

namespace CoachOS.Tests.Mappings;

[TestFixture]
public class ApplicationMapperTests
{
    private ApplicationMapper _mapper = null!;

    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid TrainerId = Guid.NewGuid();
    private static readonly Guid ClubId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _mapper = new ApplicationMapper();
    }

    // ── ToWeeklyTemplateEntry ────────────────────────────────────────────────

    [Test]
    public void ToWeeklyTemplateEntry_MapsAllFields()
    {
        WeeklyTemplateEntryRequest request = new()
        {
            DayOfWeek = 3,
            StartTime = "09:30",
            EndTime = "10:30",
            TrainerId = TrainerId,
            CourtName = "Baan 2",
            MaxStudents = 6,
        };

        LessonSerie series = new() { Id = Guid.NewGuid(), OrganizationId = OrgId };

        var entry = _mapper.ToWeeklyTemplateEntry(request, series);

        entry.LessonSerieId.Should().Be(series.Id);
        entry.DayOfWeek.Should().Be(3);
        entry.StartTime.Should().Be(new TimeOnly(9, 30));
        entry.EndTime.Should().Be(new TimeOnly(10, 30));
        entry.TrainerId.Should().Be(TrainerId);
        entry.CourtName.Should().Be("Baan 2");
        entry.MaxStudents.Should().Be(6);
    }

    // ── ToWeeklyTemplateEntryDto ─────────────────────────────────────────────

    [Test]
    public void ToWeeklyTemplateEntryDto_MapsAllFields()
    {
        var entryId = Guid.NewGuid();
        WeeklyTemplateEntry entry = new()
        {
            Id = entryId,
            DayOfWeek = 1,
            StartTime = new TimeOnly(14, 0),
            EndTime = new TimeOnly(15, 30),
            TrainerId = TrainerId,
            CourtName = "Padel 1",
            MaxStudents = 4,
        };

        var dto = _mapper.ToWeeklyTemplateEntryDto(entry);

        dto.Id.Should().Be(entryId);
        dto.DayOfWeek.Should().Be(1);
        dto.StartTime.Should().Be("14:00");
        dto.EndTime.Should().Be("15:30");
        dto.TrainerId.Should().Be(TrainerId);
        dto.CourtName.Should().Be("Padel 1");
        dto.MaxStudents.Should().Be(4);
    }

    // ── ToLesson — MaxStudents ───────────────────────────────────────────────

    [Test]
    public void ToLesson_MapsMaxStudentsFromRequest()
    {
        CreateLessonRequest request = new()
        {
            TrainerId = TrainerId,
            Date = "2026-06-15",
            StartTime = "10:00",
            EndTime = "11:00",
            CourtName = "Baan 1",
            MaxStudents = 8,
        };

        LessonSerie series = new() { Id = Guid.NewGuid(), OrganizationId = OrgId };

        var lesson = _mapper.ToLesson(request, series);

        lesson.MaxStudents.Should().Be(8);
    }

    // ── ToLessonSerie — MaxRegistrations ─────────────────────────────────────

    [Test]
    public void ToLessonSerie_MapsMaxRegistrations()
    {
        CreateLessonSerieRequest request = new()
        {
            Name = "Test",
            Price = 100m,
            StartDate = "2026-06-01",
            EndDate = "2026-08-31",
            TennisClubId = ClubId,
            MaxRegistrations = 25,
            Lessons = [],
        };

        var series = _mapper.ToLessonSerie(request, OrgId);

        series.MaxRegistrations.Should().Be(25);
    }

    [Test]
    public void ToLessonSerie_MaxRegistrations_NullWhenNotProvided()
    {
        CreateLessonSerieRequest request = new()
        {
            Name = "Test",
            Price = 100m,
            StartDate = "2026-06-01",
            EndDate = "2026-08-31",
            TennisClubId = ClubId,
            Lessons = [],
        };

        var series = _mapper.ToLessonSerie(request, OrgId);

        series.MaxRegistrations.Should().BeNull();
    }

    // ── ToTrainerAvailability ────────────────────────────────────────────────

    [Test]
    public void ToTrainerAvailability_MapsAllFields()
    {
        Guid orgId = Guid.NewGuid();
        CreateTrainerAvailabilityRequest request = new(Guid.NewGuid(), Guid.NewGuid(), 2, "17:30", "21:00");

        TrainerAvailability entity = _mapper.ToTrainerAvailability(request, orgId);

        entity.Id.Should().NotBeEmpty();
        entity.OrganizationId.Should().Be(orgId);
        entity.TrainerId.Should().Be(request.TrainerId);
        entity.TennisClubId.Should().Be(request.TennisClubId);
        entity.DayOfWeek.Should().Be(2);
        entity.StartTime.Should().Be(new TimeOnly(17, 30));
        entity.EndTime.Should().Be(new TimeOnly(21, 0));
        entity.IsActive.Should().BeTrue();
    }

    [Test]
    public void ToTrainerAvailabilityDto_MapsAllFields()
    {
        TrainerAvailability entity = new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            TrainerId = Guid.NewGuid(),
            TennisClubId = Guid.NewGuid(),
            DayOfWeek = 4,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(12, 30),
            IsActive = true,
            TennisClub = new TennisClub { Name = "TC Demo" },
        };

        TrainerAvailabilityDto dto = _mapper.ToTrainerAvailabilityDto(entity);

        dto.Id.Should().Be(entity.Id);
        dto.TrainerId.Should().Be(entity.TrainerId);
        dto.TennisClubId.Should().Be(entity.TennisClubId);
        dto.TennisClubName.Should().Be("TC Demo");
        dto.DayOfWeek.Should().Be(4);
        dto.StartTime.Should().Be("09:00");
        dto.EndTime.Should().Be("12:30");
    }
}
