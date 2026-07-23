using CoachOS.Application.Pricing;
using CoachOS.Application.Students;
using CoachOS.Application.Students.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Services;

/// <summary>
/// Bewijst dat de getoonde lesprijs uit <see cref="IPricingService"/> komt en niet
/// langer uit de gedupliceerde formule <c>series.Price * groepsgrootte</c>.
/// </summary>
[TestFixture]
public class StudentLessonsServiceTests
{
    private Mock<IScheduleAssignmentRepository> _assignmentRepo = null!;
    private Mock<IPaymentRepository> _paymentRepo = null!;
    private Mock<IPricingService> _pricingService = null!;
    private Mock<IUserLookupService> _userLookup = null!;
    private StudentLessonsService _sut = null!;

    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid SeriesId = Guid.NewGuid();
    private static readonly Guid SlotId = Guid.NewGuid();

    private const string Email = "alice@test.com";

    /// <summary>Totaal uit de prijsmatrix — bewust géén veelvoud van de legacy serieprijs.</summary>
    private const decimal MatrixTotal = 137.50m;

    private const decimal SeriesPrice = 40m;

    private Result<PriceBreakdown> _priceResult = null!;

    [SetUp]
    public void SetUp()
    {
        _assignmentRepo = new Mock<IScheduleAssignmentRepository>();
        _paymentRepo = new Mock<IPaymentRepository>();
        _pricingService = new Mock<IPricingService>();
        _userLookup = new Mock<IUserLookupService>();

        SetupPrice(MatrixTotal, groupSize: 3);
        _pricingService
            .Setup(p => p.CalculateForGroupAsync(
                It.IsAny<Guid>(), It.IsAny<IReadOnlyList<Enrollment>>(), It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(_priceResult));

        _userLookup.Setup(u => u.GetUserNamesByIdsAsync(
                It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, string>());
        _paymentRepo.Setup(p => p.GetLatestStatusByEnrollmentIdsAsync(
                It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, PaymentStatus>());

        _sut = new StudentLessonsService(
            _assignmentRepo.Object,
            _paymentRepo.Object,
            _pricingService.Object,
            _userLookup.Object);
    }

    private void SetupPrice(decimal total, int groupSize)
        => _priceResult = Result<PriceBreakdown>.Ok(new PriceBreakdown
        {
            Total = total,
            GroupSize = groupSize,
            UsedLegacyPrice = false,
        });

    [Test]
    public async Task GetMyLessonsAsync_GroupLesson_ReturnsPriceFromPricingService_NotSeriesPriceTimesGroupSize()
    {
        // Arrange: groep van 3. Legacy formule zou 3 × 40 = 120 tonen.
        ScheduleAssignment assignment = BuildGroupAssignment(out Enrollment leader, memberCount: 3);
        _assignmentRepo.Setup(r => r.GetByContactEmailAsync(Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScheduleAssignment> { assignment });

        // Act
        Result<List<StudentLessonDto>> result = await _sut.GetMyLessonsAsync(Email, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().ContainSingle();
        result.Value[0].Price.Should().Be(MatrixTotal,
            "de prijs moet uit IPricingService komen");
        result.Value[0].Price.Should().NotBe(SeriesPrice * 3);
        result.Value[0].GroupSize.Should().Be(3);

        // De volledige groep (leider inbegrepen) moet aan de prijsberekening gevoerd zijn.
        _pricingService.Verify(p => p.CalculateForGroupAsync(
                SeriesId,
                It.Is<IReadOnlyList<Enrollment>>(l => l.Count == 3 && l.Any(e => e.Id == leader.Id)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task GetMyLessonsAsync_SoloLesson_PassesSingleEnrollmentToPricingService()
    {
        Enrollment enrollment = PlanningServiceTests.BuildEnrollment("Alice", OrgId, SeriesId);
        enrollment.StudentEmail = Email;
        SetupPrice(65m, groupSize: 1);

        ScheduleAssignment assignment = BuildAssignment();
        assignment.EnrollmentId = enrollment.Id;
        assignment.Enrollment = enrollment;

        _assignmentRepo.Setup(r => r.GetByContactEmailAsync(Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScheduleAssignment> { assignment });

        Result<List<StudentLessonDto>> result = await _sut.GetMyLessonsAsync(Email, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value![0].Price.Should().Be(65m);

        _pricingService.Verify(p => p.CalculateForGroupAsync(
                SeriesId,
                It.Is<IReadOnlyList<Enrollment>>(l => l.Count == 1 && l[0].Id == enrollment.Id),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task GetMyLessonsAsync_PricingFails_PropagatesError()
    {
        ScheduleAssignment assignment = BuildGroupAssignment(out _, memberCount: 2);
        _assignmentRepo.Setup(r => r.GetByContactEmailAsync(Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScheduleAssignment> { assignment });

        _priceResult = Result<PriceBreakdown>.Fail(
            new Error(ErrorCodes.NotFound, "Lessenreeks niet gevonden."));

        Result<List<StudentLessonDto>> result = await _sut.GetMyLessonsAsync(Email, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Message == "Lessenreeks niet gevonden.");
    }

    [Test]
    public async Task GetMyLessonsAsync_SharedContactAddress_ListsEachParticipantByName()
    {
        ScheduleAssignment lotte = AssignmentForParticipant("Lotte Peeters", "ouder@example.com");
        ScheduleAssignment sofie = AssignmentForParticipant("Sofie Peeters", "ouder@example.com");
        SetupPrice(65m, groupSize: 1);

        _assignmentRepo
            .Setup(r => r.GetByContactEmailAsync("ouder@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScheduleAssignment> { lotte, sofie });

        Result<List<StudentLessonDto>> result =
            await _sut.GetMyLessonsAsync("ouder@example.com", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Select(l => l.ParticipantName)
            .Should().BeEquivalentTo("Lotte Peeters", "Sofie Peeters");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static ScheduleAssignment AssignmentForParticipant(string name, string contactEmail)
    {
        ScheduleAssignment assignment = BuildAssignment();
        Enrollment enrollment = new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            LessonSerieId = SeriesId,
            StudentName = name,
            ContactEmail = contactEmail,
        };
        assignment.EnrollmentId = enrollment.Id;
        assignment.Enrollment = enrollment;
        return assignment;
    }

    private static ScheduleAssignment BuildAssignment()
    {
        LessonSerie series = PlanningServiceTests.BuildSeries(withSlots: true, SeriesId, OrgId, SlotId);
        series.Price = SeriesPrice;

        return new ScheduleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            LessonSerieId = SeriesId,
            LessonSerie = series,
            WeeklyTemplateEntryId = SlotId,
            WeeklyTemplateEntry = series.WeeklyTemplate.First(),
            Status = ScheduleAssignmentStatus.Confirmed,
        };
    }

    private static ScheduleAssignment BuildGroupAssignment(out Enrollment leader, int memberCount)
    {
        leader = PlanningServiceTests.BuildEnrollment("Alice", OrgId, SeriesId);
        leader.StudentEmail = Email;

        List<Enrollment> members = [leader];
        for (int i = 1; i < memberCount; i++)
            members.Add(PlanningServiceTests.BuildEnrollment($"Lid{i}", OrgId, SeriesId));

        EnrollmentGroup group = new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            LessonSerieId = SeriesId,
            LeaderEnrollmentId = leader.Id,
            Members = members,
        };

        ScheduleAssignment assignment = BuildAssignment();
        assignment.EnrollmentGroupId = group.Id;
        assignment.EnrollmentGroup = group;
        return assignment;
    }
}
