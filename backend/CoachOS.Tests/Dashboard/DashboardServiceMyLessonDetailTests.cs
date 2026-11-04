using CoachOS.Application.Abstractions;
using CoachOS.Application.Dashboard;
using CoachOS.Application.Dashboard.DTOs;
using CoachOS.Domain.Common;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Dashboard;

[TestFixture]
public class DashboardServiceMyLessonDetailTests
{
    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid TrainerId = Guid.NewGuid();
    private static readonly Guid SeriesId = Guid.NewGuid();
    private static readonly Guid SlotA = Guid.NewGuid();
    private static readonly Guid SlotB = Guid.NewGuid();

    private Mock<ILessonRepository> _lessons = null!;
    private Mock<IScheduleAssignmentRepository> _assignments = null!;
    private Mock<ILessonInvitationRepository> _invitations = null!;
    private DashboardService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _lessons = new Mock<ILessonRepository>();
        _assignments = new Mock<IScheduleAssignmentRepository>();
        _invitations = new Mock<ILessonInvitationRepository>();
        _sut = new DashboardService(
            Mock.Of<ILessonSerieRepository>(), _lessons.Object, Mock.Of<IEnrollmentRepository>(),
            Mock.Of<ITennisClubRepository>(), Mock.Of<IUserLookupService>(),
            Mock.Of<IAssignmentConfirmationTokenRepository>(), _assignments.Object, _invitations.Object,
            Mock.Of<ICampRepository>(), TimeProvider.System);
    }

    private static Enrollment Student(string name, EnrollmentStatus status = EnrollmentStatus.Confirmed) =>
        new() { Id = Guid.NewGuid(), StudentName = name, Status = status };

    private static Lesson SeriesLesson(Guid? slot) => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = OrgId,
        TrainerId = TrainerId,
        LessonSerieId = SeriesId,
        WeeklyTemplateEntryId = slot,
        Date = new DateOnly(2026, 10, 21),
        StartTime = new TimeOnly(16, 0),
        EndTime = new TimeOnly(17, 30),
        CourtName = "Baan 5",
        MaxStudents = 8,
        LessonSerie = new LessonSerie
        {
            Id = SeriesId,
            Name = "Jeugdtraining",
            Level = LessonLevel.Intermediate,
            TennisClub = new TennisClub { Name = "TC De Aces", Address = "Sportlaan 12, 2000 Antwerpen" },
        },
    };

    private void GivenLesson(Lesson lesson) =>
        _lessons.Setup(r => r.GetForTrainerAsync(
                lesson.Id, TrainerId, It.IsAny<IReadOnlyList<Guid>>(), OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

    [Test]
    public async Task GetMyLesson_UnknownOrForeignLesson_ReturnsNotFound()
    {
        Guid id = Guid.NewGuid();
        _lessons.Setup(r => r.GetForTrainerAsync(
                id, TrainerId, It.IsAny<IReadOnlyList<Guid>>(), OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lesson?)null);

        Result<MyLessonDetailDto> result = await _sut.GetMyLessonAsync(OrgId, TrainerId, id, []);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.NotFound);
    }

    [Test]
    public async Task GetMyLesson_SeriesLesson_MapsLessonFields()
    {
        Lesson lesson = SeriesLesson(SlotA);
        GivenLesson(lesson);
        _assignments.Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        MyLessonDetailDto dto = (await _sut.GetMyLessonAsync(OrgId, TrainerId, lesson.Id, [])).Value!;

        dto.SeriesName.Should().Be("Jeugdtraining");
        dto.Date.Should().Be("2026-10-21");
        dto.StartTime.Should().Be("16:00");
        dto.EndTime.Should().Be("17:30");
        dto.DurationMinutes.Should().Be(90);
        dto.CourtName.Should().Be("Baan 5");
        dto.TennisClubName.Should().Be("TC De Aces");
        dto.TennisClubAddress.Should().Be("Sportlaan 12, 2000 Antwerpen");
        dto.Level.Should().Be((int)LessonLevel.Intermediate);
        dto.MaxParticipants.Should().Be(8);
    }

    [Test]
    public async Task GetMyLesson_WithPlanning_ListsOnlyPlayersOfThisSlot()
    {
        Lesson lesson = SeriesLesson(SlotA);
        GivenLesson(lesson);

        Enrollment leader = Student("Lotte");
        Enrollment sibling = Student("Noah");
        Enrollment cancelledSibling = Student("Emma", EnrollmentStatus.Cancelled);
        _assignments.Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                // Groep in dit slot: leden worden uitgeklapt, geannuleerde leden niet.
                new ScheduleAssignment
                {
                    WeeklyTemplateEntryId = SlotA,
                    Status = ScheduleAssignmentStatus.Confirmed,
                    EnrollmentGroup = new EnrollmentGroup { Members = [leader, sibling, cancelledSibling] },
                },
                // Solo, wacht nog op bevestiging.
                new ScheduleAssignment
                {
                    WeeklyTemplateEntryId = SlotA,
                    Status = ScheduleAssignmentStatus.AwaitingConfirmation,
                    Enrollment = Student("Liam"),
                },
                // Geweigerd: niet tonen.
                new ScheduleAssignment
                {
                    WeeklyTemplateEntryId = SlotA,
                    Status = ScheduleAssignmentStatus.Declined,
                    Enrollment = Student("Mila"),
                },
                // Ander weekslot: niet tonen.
                new ScheduleAssignment
                {
                    WeeklyTemplateEntryId = SlotB,
                    Status = ScheduleAssignmentStatus.Confirmed,
                    Enrollment = Student("Sam"),
                },
            ]);

        MyLessonDetailDto dto = (await _sut.GetMyLessonAsync(OrgId, TrainerId, lesson.Id, [])).Value!;

        dto.Participants.Select(p => (p.Name, p.Status)).Should().BeEquivalentTo(
        [
            ("Liam", "awaiting"),
            ("Lotte", "confirmed"),
            ("Noah", "confirmed"),
        ]);
    }

    [Test]
    public async Task GetMyLesson_StandaloneLesson_ListsInvitationsWithStatus()
    {
        Lesson lesson = new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            TrainerId = TrainerId,
            LessonSerieId = null,
            Date = new DateOnly(2026, 10, 27),
            StartTime = new TimeOnly(20, 0),
            EndTime = new TimeOnly(21, 0),
            Level = LessonLevel.Advanced,
            Notes = "Focus op service",
            TennisClub = new TennisClub { Name = "Padel Center", Address = "Louizalaan 45" },
        };
        GivenLesson(lesson);
        _invitations.Setup(r => r.GetByLessonAsync(lesson.Id, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new LessonInvitation { Email = "tom@x.be", FirstName = "Tom", Status = LessonInvitationStatus.Accepted, InvitationSentAt = DateTime.UtcNow.AddHours(-2) },
                new LessonInvitation { Email = "anna@x.be", Status = LessonInvitationStatus.Pending, InvitationSentAt = DateTime.UtcNow.AddHours(-1) },
                new LessonInvitation { Email = "ben@x.be", FirstName = "Ben", Status = LessonInvitationStatus.Declined, InvitationSentAt = DateTime.UtcNow },
            ]);

        MyLessonDetailDto dto = (await _sut.GetMyLessonAsync(OrgId, TrainerId, lesson.Id, [])).Value!;

        dto.SeriesId.Should().BeNull();
        dto.Level.Should().Be((int)LessonLevel.Advanced);
        dto.Notes.Should().Be("Focus op service");
        dto.TennisClubName.Should().Be("Padel Center");
        dto.Participants.Select(p => (p.Name, p.Status)).Should().Equal(
            ("Tom", "accepted"), ("anna@x.be", "pending"), ("Ben", "declined"));
        _assignments.VerifyNoOtherCalls();
    }
}
