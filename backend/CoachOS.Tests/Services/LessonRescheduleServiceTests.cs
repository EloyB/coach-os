using CoachOS.Application.Abstractions;
using CoachOS.Application.LessonReschedule;
using CoachOS.Application.LessonReschedule.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Services;

[TestFixture]
public class LessonRescheduleServiceTests
{
    private Mock<ILessonRepository> _lessonRepo = null!;
    private Mock<ILessonInvitationRepository> _invitationRepo = null!;
    private Mock<IEnrollmentRepository> _enrollmentRepo = null!;
    private Mock<ILessonSerieRepository> _serieRepo = null!;
    private Mock<IEmailService> _emailService = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private LessonRescheduleService _service = null!;

    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid TrainerId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _lessonRepo = new Mock<ILessonRepository>();
        _invitationRepo = new Mock<ILessonInvitationRepository>();
        _enrollmentRepo = new Mock<IEnrollmentRepository>();
        _serieRepo = new Mock<ILessonSerieRepository>();
        _emailService = new Mock<IEmailService>();
        _unitOfWork = new Mock<IUnitOfWork>();

        _service = new LessonRescheduleService(
            _lessonRepo.Object,
            _invitationRepo.Object,
            _enrollmentRepo.Object,
            _serieRepo.Object,
            _unitOfWork.Object,
            _emailService.Object,
            NullLogger<LessonRescheduleService>.Instance);

        _lessonRepo
            .Setup(r => r.FindTrainerConflictAsync(
                It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<TimeOnly>(), It.IsAny<TimeOnly>(),
                It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lesson?)null);

        _invitationRepo
            .Setup(r => r.GetByLessonAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<LessonInvitation>());

        _enrollmentRepo
            .Setup(r => r.GetBySeriesAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Enrollment>());
    }

    private static Lesson BuildStandaloneLesson(bool cancelled = false)
        => new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            LessonSerieId = null,
            Date = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
            TrainerId = TrainerId,
            CourtName = "Baan 1",
            MaxStudents = 4,
            Level = LessonLevel.Beginner,
            IsCancelled = cancelled,
        };

    private static RescheduleLessonRequest BuildRequest(int daysFromNow = 14, string? reason = null)
        => new(
            DateOnly.FromDateTime(DateTime.UtcNow).AddDays(daysFromNow).ToString("yyyy-MM-dd"),
            "14:00",
            "15:00",
            reason);

    [Test]
    public async Task RescheduleAsync_StandaloneLesson_UpdatesLessonInPlace()
    {
        Lesson lesson = BuildStandaloneLesson();
        Guid originalId = lesson.Id;
        _lessonRepo
            .Setup(r => r.GetByIdInOrganizationAsync(lesson.Id, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        Result<RescheduleLessonResultDto> result = await _service.RescheduleAsync(
            OrgId, lesson.Id, BuildRequest(reason: "Trainer ziek"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Zelfde les, in-place verplaatst — geen nieuwe les, geen annulering.
        result.Value!.NewLessonId.Should().Be(originalId);
        lesson.IsCancelled.Should().BeFalse();
        lesson.RescheduledToLessonId.Should().BeNull();
        lesson.StartTime.Should().Be(new TimeOnly(14, 0));
        lesson.EndTime.Should().Be(new TimeOnly(15, 0));

        _lessonRepo.Verify(r => r.AddAsync(It.IsAny<Lesson>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _invitationRepo.Verify(r => r.ReassignToLessonAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task RescheduleAsync_AlreadyCancelled_ReturnsValidationFailure()
    {
        Lesson lesson = BuildStandaloneLesson(cancelled: true);
        _lessonRepo
            .Setup(r => r.GetByIdInOrganizationAsync(lesson.Id, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        Result<RescheduleLessonResultDto> result = await _service.RescheduleAsync(
            OrgId, lesson.Id, BuildRequest(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.Validation);
        _lessonRepo.Verify(r => r.AddAsync(It.IsAny<Lesson>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task RescheduleAsync_NotFound_ReturnsNotFound()
    {
        Guid missingId = Guid.NewGuid();
        _lessonRepo
            .Setup(r => r.GetByIdInOrganizationAsync(missingId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lesson?)null);

        Result<RescheduleLessonResultDto> result = await _service.RescheduleAsync(
            OrgId, missingId, BuildRequest(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.NotFound);
    }

    [Test]
    public async Task RescheduleAsync_TrainerConflict_ReturnsConflict()
    {
        Lesson lesson = BuildStandaloneLesson();
        _lessonRepo
            .Setup(r => r.GetByIdInOrganizationAsync(lesson.Id, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);
        _lessonRepo
            .Setup(r => r.FindTrainerConflictAsync(
                TrainerId, It.IsAny<DateOnly>(), It.IsAny<TimeOnly>(), It.IsAny<TimeOnly>(),
                lesson.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Lesson { Id = Guid.NewGuid() });

        Result<RescheduleLessonResultDto> result = await _service.RescheduleAsync(
            OrgId, lesson.Id, BuildRequest(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.Conflict);
        _lessonRepo.Verify(r => r.AddAsync(It.IsAny<Lesson>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task RescheduleAsync_StandaloneLesson_SendsEmailToActiveInvitees()
    {
        Lesson lesson = BuildStandaloneLesson();
        _lessonRepo
            .Setup(r => r.GetByIdInOrganizationAsync(lesson.Id, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);

        // Na reassign-call: 2 actieve + 1 declined invitee — declined mag GEEN mail krijgen.
        List<LessonInvitation> invitees =
        [
            new() { Email = "a@x.be", FirstName = "Anna", Status = LessonInvitationStatus.Pending },
            new() { Email = "b@x.be", FirstName = "Bram", Status = LessonInvitationStatus.Accepted },
            new() { Email = "c@x.be", FirstName = "Cara", Status = LessonInvitationStatus.Declined },
        ];
        _invitationRepo
            .Setup(r => r.GetByLessonAsync(It.IsAny<Guid>(), OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitees);

        // Oude datum/tijd vastleggen vóór de in-place verplaatsing (de mail toont die).
        DateOnly oldDate = lesson.Date;
        TimeOnly oldStart = lesson.StartTime;

        Result<RescheduleLessonResultDto> result = await _service.RescheduleAsync(
            OrgId, lesson.Id, BuildRequest(reason: "Andere zaal"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.NotifiedCount.Should().Be(2);

        _emailService.Verify(e => e.SendLessonRescheduledAsync(
                "a@x.be", "Anna", null,
                oldDate, oldStart,
                It.IsAny<DateOnly>(), It.IsAny<TimeOnly>(), It.IsAny<TimeOnly>(),
                "Baan 1", "Andere zaal",
                It.IsAny<CancellationToken>()),
            Times.Once);
        _emailService.Verify(e => e.SendLessonRescheduledAsync(
                "c@x.be", It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<DateOnly>(), It.IsAny<TimeOnly>(),
                It.IsAny<DateOnly>(), It.IsAny<TimeOnly>(), It.IsAny<TimeOnly>(),
                It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task RescheduleAsync_SerieInstance_SendsEmailToActiveSeriesEnrollments()
    {
        Guid seriesId = Guid.NewGuid();
        Lesson lesson = BuildStandaloneLesson();
        lesson.LessonSerieId = seriesId;
        _lessonRepo
            .Setup(r => r.GetByIdInOrganizationAsync(lesson.Id, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lesson);
        _serieRepo
            .Setup(r => r.GetByIdAsync(seriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LessonSerie { Id = seriesId, Name = "Beginners maandag" });

        List<Enrollment> enrollments =
        [
            new() { StudentEmail = "x@y.be", ContactEmail = "x@y.be", StudentName = "Xan", Status = EnrollmentStatus.Confirmed },
            new() { StudentEmail = "y@y.be", ContactEmail = "y@y.be", StudentName = "Yana", Status = EnrollmentStatus.Pending },
            new() { StudentEmail = "z@y.be", ContactEmail = "z@y.be", StudentName = "Zoë", Status = EnrollmentStatus.Cancelled },
        ];
        _enrollmentRepo
            .Setup(r => r.GetBySeriesAsync(seriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrollments);

        Result<RescheduleLessonResultDto> result = await _service.RescheduleAsync(
            OrgId, lesson.Id, BuildRequest(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.NotifiedCount.Should().Be(2);

        _emailService.Verify(e => e.SendLessonRescheduledAsync(
                "x@y.be", "Xan", "Beginners maandag",
                It.IsAny<DateOnly>(), It.IsAny<TimeOnly>(),
                It.IsAny<DateOnly>(), It.IsAny<TimeOnly>(), It.IsAny<TimeOnly>(),
                It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task RescheduleAsync_OtherOrganization_ReturnsNotFound()
    {
        Guid otherLessonId = Guid.NewGuid();
        _lessonRepo
            .Setup(r => r.GetByIdInOrganizationAsync(otherLessonId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lesson?)null);

        Result<RescheduleLessonResultDto> result = await _service.RescheduleAsync(
            OrgId, otherLessonId, BuildRequest(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.NotFound);
    }
}
