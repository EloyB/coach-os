using CoachOS.Application.Configuration;
using CoachOS.Application.Planning;
using CoachOS.Application.Pricing;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Services;

/// <summary>
/// Bundeling van planningsmails: deelnemers die hetzelfde contactadres delen krijgen
/// één mail met een eigen bevestigingsknop per deelnemer, in plaats van één mail elk.
/// </summary>
[TestFixture]
public class ConfirmationBundlingTests
{
    private Mock<ILessonSerieRepository> _seriesRepo = null!;
    private Mock<IScheduleAssignmentRepository> _assignmentRepo = null!;
    private Mock<IAssignmentConfirmationTokenRepository> _tokenRepo = null!;
    private Mock<IPaymentRepository> _paymentRepo = null!;
    private Mock<IEmailService> _emailService = null!;
    private Mock<IPricingService> _pricingService = null!;
    private Mock<IOptions<AppOptions>> _appOptions = null!;
    private Mock<ILogger<ConfirmationOrchestrationService>> _logger = null!;
    private ConfirmationOrchestrationService _service = null!;

    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid SeriesId = Guid.NewGuid();
    private static readonly Guid SlotId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _seriesRepo = new Mock<ILessonSerieRepository>();
        _assignmentRepo = new Mock<IScheduleAssignmentRepository>();
        _tokenRepo = new Mock<IAssignmentConfirmationTokenRepository>();
        _paymentRepo = new Mock<IPaymentRepository>();
        _emailService = new Mock<IEmailService>();
        _pricingService = new Mock<IPricingService>();
        _appOptions = new Mock<IOptions<AppOptions>>();
        _appOptions.Setup(o => o.Value).Returns(new AppOptions());
        _logger = new Mock<ILogger<ConfirmationOrchestrationService>>();

        _service = new ConfirmationOrchestrationService(
            _seriesRepo.Object,
            _assignmentRepo.Object,
            _tokenRepo.Object,
            _paymentRepo.Object,
            _emailService.Object,
            _pricingService.Object,
            _appOptions.Object,
            _logger.Object);
    }

    [Test]
    public async Task Three_Assignments_On_One_Contact_Address_Produce_One_Email()
    {
        SetUpSeriesWithAssignments(
            ("Lotte Peeters", "ouder@example.com"),
            ("Sofie Peeters", "ouder@example.com"),
            ("Jan Peeters", "ouder@example.com"));

        await _service.ConfirmScheduleAsync(SeriesId, OrgId);

        _emailService.Verify(s => s.SendScheduleConfirmationBundleAsync(
            "ouder@example.com",
            It.IsAny<string>(),
            It.Is<IReadOnlyList<ScheduleConfirmationItem>>(items => items.Count == 3),
            It.IsAny<CancellationToken>()), Times.Once);

        _emailService.Verify(s => s.SendScheduleConfirmationAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<IReadOnlyList<string>?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task A_Single_Recipient_Keeps_The_Existing_Template()
    {
        SetUpSeriesWithAssignments(("Jan Peeters", "jan@example.com"));

        await _service.ConfirmScheduleAsync(SeriesId, OrgId);

        _emailService.Verify(s => s.SendScheduleConfirmationAsync(
            "jan@example.com", "Jan Peeters", It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<IReadOnlyList<string>?>(), It.IsAny<CancellationToken>()), Times.Once);

        _emailService.Verify(s => s.SendScheduleConfirmationBundleAsync(
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<IReadOnlyList<ScheduleConfirmationItem>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task Each_Participant_Keeps_Their_Own_Confirmation_Link()
    {
        SetUpSeriesWithAssignments(
            ("Lotte Peeters", "ouder@example.com"),
            ("Sofie Peeters", "ouder@example.com"));

        IReadOnlyList<ScheduleConfirmationItem>? captured = null;
        _emailService
            .Setup(s => s.SendScheduleConfirmationBundleAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<IReadOnlyList<ScheduleConfirmationItem>>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, IReadOnlyList<ScheduleConfirmationItem>, CancellationToken>(
                (_, _, items, _) => captured = items)
            .Returns(Task.CompletedTask);

        await _service.ConfirmScheduleAsync(SeriesId, OrgId);

        captured!.Select(i => i.ConfirmationUrl).Distinct().Should().HaveCount(2);
    }

    private void SetUpSeriesWithAssignments(params (string Name, string ContactEmail)[] people)
    {
        LessonSerie series = PlanningServiceTests.BuildSeries(withSlots: true, SeriesId, OrgId, SlotId);
        series.PlanningStatus = PlanningStatus.Planning;

        List<ScheduleAssignment> assignments = people.Select(p =>
        {
            Guid enrollmentId = Guid.NewGuid();
            return new ScheduleAssignment
            {
                Id = Guid.NewGuid(),
                OrganizationId = OrgId,
                LessonSerieId = SeriesId,
                WeeklyTemplateEntryId = SlotId,
                EnrollmentId = enrollmentId,
                Enrollment = new Enrollment
                {
                    Id = enrollmentId,
                    OrganizationId = OrgId,
                    LessonSerieId = SeriesId,
                    StudentName = p.Name,
                    ContactEmail = p.ContactEmail,
                },
                Status = ScheduleAssignmentStatus.Proposed,
            };
        }).ToList();

        _seriesRepo.Setup(r => r.GetByIdAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);
        _assignmentRepo.Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignments);
    }
}
