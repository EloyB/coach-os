using CoachOS.Application.Enrollments;
using CoachOS.Application.Enrollments.DTOs;
using CoachOS.Application.Mappings;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Services;

[TestFixture]
public class EnrollmentPriceOptionTests
{
    private Mock<IEnrollmentRepository> _enrollmentRepo = null!;
    private Mock<IEnrollmentFormRepository> _enrollmentFormRepo = null!;
    private Mock<ILessonSerieRepository> _lessonSeriesRepo = null!;
    private Mock<IEnrollmentGroupRepository> _enrollmentGroupRepo = null!;
    private Mock<ITimeSlotPreferenceRepository> _timeSlotPreferenceRepo = null!;
    private Mock<IOrganizationSettingsRepository> _orgSettingsRepo = null!;
    private Mock<IUserLookupService> _userLookup = null!;
    private Mock<IEmailOutboxRepository> _emailOutboxRepository = null!;
    private Mock<ILessonSeriePriceRepository> _priceRepo = null!;
    private Mock<ILogger<EnrollmentService>> _logger = null!;
    private ApplicationMapper _mapper = null!;
    private EnrollmentService _service = null!;

    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid SeriesId = Guid.NewGuid();
    private static readonly Guid OptionA = Guid.NewGuid();
    private static readonly Guid OptionB = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _enrollmentRepo = new Mock<IEnrollmentRepository>();
        _enrollmentFormRepo = new Mock<IEnrollmentFormRepository>();
        _lessonSeriesRepo = new Mock<ILessonSerieRepository>();
        _enrollmentGroupRepo = new Mock<IEnrollmentGroupRepository>();
        _timeSlotPreferenceRepo = new Mock<ITimeSlotPreferenceRepository>();
        _orgSettingsRepo = new Mock<IOrganizationSettingsRepository>();
        _userLookup = new Mock<IUserLookupService>();
        _emailOutboxRepository = new Mock<IEmailOutboxRepository>();
        _priceRepo = new Mock<ILessonSeriePriceRepository>();
        _logger = new Mock<ILogger<EnrollmentService>>();
        _mapper = new ApplicationMapper();

        _service = new EnrollmentService(
            _enrollmentRepo.Object, _enrollmentFormRepo.Object, _lessonSeriesRepo.Object,
            _enrollmentGroupRepo.Object, _timeSlotPreferenceRepo.Object, _orgSettingsRepo.Object,
            _userLookup.Object, _emailOutboxRepository.Object, _priceRepo.Object, _mapper, _logger.Object,
            TimeProvider.System);

        // Geen duplicaat; reeks bevat OptionA en OptionB.
        _enrollmentRepo
            .Setup(r => r.IsDuplicateParticipantExceptAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<DateOnly?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _priceRepo
            .Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LessonSeriePrice>
            {
                new() { Id = OptionA, LessonSerieId = SeriesId, Label = "Groep van 3", TotalPrice = 100 },
                new() { Id = OptionB, LessonSerieId = SeriesId, Label = "Groep van 4", TotalPrice = 90 },
            });
    }

    private static Enrollment SoloEnrollment(EnrollmentStatus status, Guid? option) => new()
    {
        Id = Guid.NewGuid(), OrganizationId = OrgId, LessonSerieId = SeriesId,
        StudentName = "Lars Peeters", ContactEmail = "lars@test.local", DateOfBirth = new DateOnly(2000, 1, 1),
        Status = status, SelectedPriceOptionId = option,
    };

    private static UpdateBasicEnrollmentRequest Request(Guid? option) => new()
    {
        StudentName = "Lars Peeters", ContactEmail = "lars@test.local",
        DateOfBirth = "2000-01-01", IsOpenToGrouping = false, SelectedPriceOptionId = option,
    };

    [Test]
    public async Task Solo_Pending_ChangeOption_Persists()
    {
        Enrollment e = SoloEnrollment(EnrollmentStatus.Pending, OptionA);
        _enrollmentRepo.Setup(r => r.GetByIdAsync(e.Id, OrgId, It.IsAny<CancellationToken>())).ReturnsAsync(e);

        Result<LessonSerieEnrollmentDto> result =
            await _service.UpdateBasicEnrollmentAsync(SeriesId, e.Id, OrgId, Request(OptionB), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        e.SelectedPriceOptionId.Should().Be(OptionB);
        _enrollmentRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Group_Pending_ChangeOption_AppliesToAllMembers()
    {
        Guid groupId = Guid.NewGuid();
        Enrollment leader = SoloEnrollment(EnrollmentStatus.Pending, OptionA);
        leader.EnrollmentGroupId = groupId;
        Enrollment member = SoloEnrollment(EnrollmentStatus.Pending, OptionA);
        member.EnrollmentGroupId = groupId;
        EnrollmentGroup group = new() { Id = groupId, Members = new List<Enrollment> { leader, member } };
        leader.EnrollmentGroup = group; member.EnrollmentGroup = group;

        _enrollmentRepo.Setup(r => r.GetByIdAsync(leader.Id, OrgId, It.IsAny<CancellationToken>())).ReturnsAsync(leader);
        _enrollmentRepo.Setup(r => r.GetByIdWithGroupAsync(leader.Id, OrgId, It.IsAny<CancellationToken>())).ReturnsAsync(leader);

        Result<LessonSerieEnrollmentDto> result =
            await _service.UpdateBasicEnrollmentAsync(SeriesId, leader.Id, OrgId, Request(OptionB), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        leader.SelectedPriceOptionId.Should().Be(OptionB);
        member.SelectedPriceOptionId.Should().Be(OptionB);
    }

    [Test]
    public async Task Confirmed_ChangeOption_ReturnsConflict()
    {
        Enrollment e = SoloEnrollment(EnrollmentStatus.Confirmed, OptionA);
        _enrollmentRepo.Setup(r => r.GetByIdAsync(e.Id, OrgId, It.IsAny<CancellationToken>())).ReturnsAsync(e);

        Result<LessonSerieEnrollmentDto> result =
            await _service.UpdateBasicEnrollmentAsync(SeriesId, e.Id, OrgId, Request(OptionB), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Code == ErrorCodes.Conflict);
        e.SelectedPriceOptionId.Should().Be(OptionA);
        _enrollmentRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Cancelled_ChangeOption_ReturnsConflict()
    {
        // Frontend maakt geannuleerde inschrijvingen read-only; de backend-gate moet ze ook
        // blokkeren zodat een directe API-call de prijsoptie niet stil wijzigt.
        Enrollment e = SoloEnrollment(EnrollmentStatus.Cancelled, OptionA);
        _enrollmentRepo.Setup(r => r.GetByIdAsync(e.Id, OrgId, It.IsAny<CancellationToken>())).ReturnsAsync(e);

        Result<LessonSerieEnrollmentDto> result =
            await _service.UpdateBasicEnrollmentAsync(SeriesId, e.Id, OrgId, Request(OptionB), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Code == ErrorCodes.Conflict);
        e.SelectedPriceOptionId.Should().Be(OptionA);
        _enrollmentRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task PendingPayment_ChangeOption_ReturnsConflict()
    {
        Enrollment e = SoloEnrollment(EnrollmentStatus.PendingPayment, OptionA);
        _enrollmentRepo.Setup(r => r.GetByIdAsync(e.Id, OrgId, It.IsAny<CancellationToken>())).ReturnsAsync(e);

        Result<LessonSerieEnrollmentDto> result =
            await _service.UpdateBasicEnrollmentAsync(SeriesId, e.Id, OrgId, Request(OptionB), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Code == ErrorCodes.Conflict);
    }

    [Test]
    public async Task InvalidOption_ReturnsValidation()
    {
        Enrollment e = SoloEnrollment(EnrollmentStatus.Pending, OptionA);
        _enrollmentRepo.Setup(r => r.GetByIdAsync(e.Id, OrgId, It.IsAny<CancellationToken>())).ReturnsAsync(e);
        Guid unknown = Guid.NewGuid();

        Result<LessonSerieEnrollmentDto> result =
            await _service.UpdateBasicEnrollmentAsync(SeriesId, e.Id, OrgId, Request(unknown), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Code == ErrorCodes.Validation);
        _enrollmentRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Confirmed_UnchangedOption_StillUpdatesBasicFields()
    {
        // Optie ongewijzigd → geen gate; basis-update (bv. telefoon) mag gewoon door, ook bij Confirmed.
        Enrollment e = SoloEnrollment(EnrollmentStatus.Confirmed, OptionA);
        _enrollmentRepo.Setup(r => r.GetByIdAsync(e.Id, OrgId, It.IsAny<CancellationToken>())).ReturnsAsync(e);
        UpdateBasicEnrollmentRequest req = Request(OptionA) with { StudentPhone = "+32470000000" };

        Result<LessonSerieEnrollmentDto> result =
            await _service.UpdateBasicEnrollmentAsync(SeriesId, e.Id, OrgId, req, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        e.StudentPhone.Should().Be("+32470000000");
        _enrollmentRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
