using CoachOS.Application.LessonSerie;
using CoachOS.Application.LessonSerie.DTOs;
using CoachOS.Application.Mappings;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Services;

/// <summary>
/// Reikwijdte van een lesmoment-wijziging: "slot" past tijd/trainer/baan/capaciteit toe op
/// het hele weekslot (WeeklyTemplateEntry + alle niet-geannuleerde lessen), zodat de planning
/// — die de template leest — meegaat. "lesson" raakt enkel die ene les.
/// </summary>
[TestFixture]
public class LessonSlotScopeUpdateTests
{
    private Mock<ILessonSerieRepository> _serieRepo = null!;
    private Mock<ILessonRepository> _lessonRepo = null!;
    private Mock<IEnrollmentRepository> _enrollmentRepo = null!;
    private Mock<ITennisClubRepository> _tennisClubRepo = null!;
    private Mock<IUserLookupService> _userLookup = null!;
    private Mock<IEmailService> _emailService = null!;
    private Mock<IMollieConnectionRepository> _mollieConnectionRepo = null!;
    private Mock<IScheduleAssignmentRepository> _scheduleAssignmentRepo = null!;
    private Mock<ITimeSlotPreferenceRepository> _timeSlotPreferenceRepo = null!;
    private ApplicationMapper _mapper = null!;
    private LessonSerieService _service = null!;

    private static readonly Guid OrgId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _serieRepo = new Mock<ILessonSerieRepository>();
        _lessonRepo = new Mock<ILessonRepository>();
        _enrollmentRepo = new Mock<IEnrollmentRepository>();
        _tennisClubRepo = new Mock<ITennisClubRepository>();
        _userLookup = new Mock<IUserLookupService>();
        _emailService = new Mock<IEmailService>();
        _mollieConnectionRepo = new Mock<IMollieConnectionRepository>();
        _scheduleAssignmentRepo = new Mock<IScheduleAssignmentRepository>();
        _timeSlotPreferenceRepo = new Mock<ITimeSlotPreferenceRepository>();
        _mapper = new ApplicationMapper();

        _service = new LessonSerieService(
            _serieRepo.Object, _lessonRepo.Object, _enrollmentRepo.Object,
            _tennisClubRepo.Object, _userLookup.Object, _emailService.Object,
            _mollieConnectionRepo.Object, _scheduleAssignmentRepo.Object,
            _timeSlotPreferenceRepo.Object, _mapper);

        _userLookup
            .Setup(u => u.IsActiveTrainerAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        // Geen trainer-/baan-conflicten.
        _lessonRepo
            .Setup(r => r.FindTrainerConflictAsync(
                It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<TimeOnly>(), It.IsAny<TimeOnly>(),
                It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lesson?)null);
        _lessonRepo
            .Setup(r => r.FindCourtConflictAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<TimeOnly>(),
                It.IsAny<TimeOnly>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lesson?)null);
    }

    private (LessonSerie Series, Lesson Edited, Lesson Sibling, Lesson CancelledSibling, WeeklyTemplateEntry Entry)
        BuildSlotScenario()
    {
        Guid seriesId = Guid.NewGuid();
        WeeklyTemplateEntry entry = new()
        {
            Id = Guid.NewGuid(), LessonSerieId = seriesId, DayOfWeek = 0,
            StartTime = new TimeOnly(18, 0), EndTime = new TimeOnly(19, 0), MaxStudents = 4,
        };
        Lesson edited = new()
        {
            Id = Guid.NewGuid(), OrganizationId = OrgId, LessonSerieId = seriesId,
            Date = new DateOnly(2026, 12, 7), StartTime = new TimeOnly(18, 0), EndTime = new TimeOnly(19, 0),
            MaxStudents = 4, WeeklyTemplateEntryId = entry.Id,
        };
        Lesson sibling = new()
        {
            Id = Guid.NewGuid(), OrganizationId = OrgId, LessonSerieId = seriesId,
            Date = new DateOnly(2026, 12, 14), StartTime = new TimeOnly(18, 0), EndTime = new TimeOnly(19, 0),
            MaxStudents = 4, WeeklyTemplateEntryId = entry.Id,
        };
        Lesson cancelledSibling = new()
        {
            Id = Guid.NewGuid(), OrganizationId = OrgId, LessonSerieId = seriesId,
            Date = new DateOnly(2026, 12, 21), StartTime = new TimeOnly(18, 0), EndTime = new TimeOnly(19, 0),
            MaxStudents = 4, WeeklyTemplateEntryId = entry.Id, IsCancelled = true,
        };
        LessonSerie series = new()
        {
            Id = seriesId, OrganizationId = OrgId, Name = "Reeks", TennisClubId = Guid.NewGuid(),
            WeeklyTemplate = new List<WeeklyTemplateEntry> { entry },
            Lessons = new List<Lesson> { edited, sibling, cancelledSibling },
        };
        _serieRepo
            .Setup(r => r.GetByIdAsync(seriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);
        _lessonRepo
            .Setup(r => r.GetByIdAsync(edited.Id, seriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(edited);
        return (series, edited, sibling, cancelledSibling, entry);
    }

    private static UpdateLessonRequest ChangeEndTo1930(string applyTo) => new()
    {
        StartTime = "18:00", EndTime = "19:30", MaxStudents = 4, ApplyTo = applyTo,
    };

    [Test]
    public async Task UpdateLessonAsync_SlotScope_UpdatesTemplateAndActiveSiblings()
    {
        (LessonSerie series, Lesson edited, Lesson sibling, Lesson cancelledSibling, WeeklyTemplateEntry entry) =
            BuildSlotScenario();

        Result<LessonDto> result = await _service.UpdateLessonAsync(
            series.Id, edited.Id, OrgId, ChangeEndTo1930("slot"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Template mee → de planning (die de template leest) toont voortaan 1,5u.
        entry.EndTime.Should().Be(new TimeOnly(19, 30));
        edited.EndTime.Should().Be(new TimeOnly(19, 30));
        sibling.EndTime.Should().Be(new TimeOnly(19, 30));
        // Geannuleerde les blijft ongemoeid.
        cancelledSibling.EndTime.Should().Be(new TimeOnly(19, 0));
        _lessonRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpdateLessonAsync_LessonScope_LeavesTemplateAndSiblingsUnchanged()
    {
        (LessonSerie series, Lesson edited, Lesson sibling, Lesson _, WeeklyTemplateEntry entry) =
            BuildSlotScenario();

        Result<LessonDto> result = await _service.UpdateLessonAsync(
            series.Id, edited.Id, OrgId, ChangeEndTo1930("lesson"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        edited.EndTime.Should().Be(new TimeOnly(19, 30));
        // Enkel deze les → template + zusjes ongemoeid; planning blijft 1u tonen (verwacht bij deze scope).
        entry.EndTime.Should().Be(new TimeOnly(19, 0));
        sibling.EndTime.Should().Be(new TimeOnly(19, 0));
    }

    // ── DeleteLessonAsync (wholeSlot) ─────────────────────────────────────────

    private (Guid SeriesId, Lesson Lesson, WeeklyTemplateEntry Entry) SetupSlotForDelete(
        ScheduleAssignmentStatus? assignmentStatus)
    {
        Guid seriesId = Guid.NewGuid();
        WeeklyTemplateEntry entry = new()
        {
            Id = Guid.NewGuid(), LessonSerieId = seriesId, DayOfWeek = 0,
            StartTime = new TimeOnly(18, 0), EndTime = new TimeOnly(19, 0),
        };
        Lesson l1 = new() { Id = Guid.NewGuid(), OrganizationId = OrgId, LessonSerieId = seriesId, WeeklyTemplateEntryId = entry.Id };
        Lesson l2 = new() { Id = Guid.NewGuid(), OrganizationId = OrgId, LessonSerieId = seriesId, WeeklyTemplateEntryId = entry.Id };
        LessonSerie series = new()
        {
            Id = seriesId, OrganizationId = OrgId, Name = "R", TennisClubId = Guid.NewGuid(),
            WeeklyTemplate = new List<WeeklyTemplateEntry> { entry },
            Lessons = new List<Lesson> { l1, l2 },
        };
        // De les die de gebruiker verwijdert (met leeg Enrollments) — aparte instance, zoals in productie.
        Lesson deleteTarget = new() { Id = l1.Id, OrganizationId = OrgId, LessonSerieId = seriesId, WeeklyTemplateEntryId = entry.Id };
        _lessonRepo
            .Setup(r => r.GetByIdWithEnrollmentsAsync(l1.Id, seriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deleteTarget);
        _serieRepo
            .Setup(r => r.GetByIdAsync(seriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);

        List<ScheduleAssignment> assignments = assignmentStatus is ScheduleAssignmentStatus st
            ? new List<ScheduleAssignment> { new() { Id = Guid.NewGuid(), WeeklyTemplateEntryId = entry.Id, Status = st } }
            : new List<ScheduleAssignment>();
        _scheduleAssignmentRepo
            .Setup(r => r.GetBySeriesAsync(seriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignments);
        _timeSlotPreferenceRepo
            .Setup(r => r.GetBySeriesAsync(seriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TimeSlotPreference>
            {
                new() { Id = Guid.NewGuid(), WeeklyTemplateEntryId = entry.Id },
            });
        return (seriesId, l1, entry);
    }

    [Test]
    public async Task DeleteLessonAsync_WholeSlot_NoConfirmed_RemovesSlotWithChildrenInOneSave()
    {
        (Guid seriesId, Lesson lesson, WeeklyTemplateEntry entry) = SetupSlotForDelete(ScheduleAssignmentStatus.Proposed);

        Result result = await _service.DeleteLessonAsync(seriesId, lesson.Id, OrgId, wholeSlot: true, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _scheduleAssignmentRepo.Verify(r => r.RemoveRange(It.IsAny<IEnumerable<ScheduleAssignment>>()), Times.Once);
        _timeSlotPreferenceRepo.Verify(r => r.RemoveRange(It.IsAny<IEnumerable<TimeSlotPreference>>()), Times.Once);
        _lessonRepo.Verify(r => r.DeleteRangeAsync(
            It.Is<IEnumerable<Lesson>>(ls => ls.Count() == 2), It.IsAny<CancellationToken>()), Times.Once);
        _serieRepo.Verify(r => r.DeleteWeeklyTemplateRangeAsync(
            It.Is<IEnumerable<WeeklyTemplateEntry>>(e => e.Contains(entry)), It.IsAny<CancellationToken>()), Times.Once);
        _serieRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task DeleteLessonAsync_WholeSlot_ConfirmedAssignment_ReturnsConflictAndDeletesNothing()
    {
        (Guid seriesId, Lesson lesson, WeeklyTemplateEntry _) = SetupSlotForDelete(ScheduleAssignmentStatus.Confirmed);

        Result result = await _service.DeleteLessonAsync(seriesId, lesson.Id, OrgId, wholeSlot: true, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ErrorCodes.Conflict);
        _serieRepo.Verify(r => r.DeleteWeeklyTemplateRangeAsync(
            It.IsAny<IEnumerable<WeeklyTemplateEntry>>(), It.IsAny<CancellationToken>()), Times.Never);
        _lessonRepo.Verify(r => r.DeleteRangeAsync(
            It.IsAny<IEnumerable<Lesson>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
