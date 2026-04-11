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
public class EnrollmentServiceTests
{
    private Mock<IEnrollmentRepository> _enrollmentRepo = null!;
    private Mock<IEnrollmentFormRepository> _enrollmentFormRepo = null!;
    private Mock<ILessonSerieRepository> _lessonSeriesRepo = null!;
    private Mock<IUserLookupService> _userLookup = null!;
    private Mock<IEmailService> _emailService = null!;
    private ApplicationMapper _mapper = null!;
    private Mock<ILogger<EnrollmentService>> _logger = null!;
    private EnrollmentService _service = null!;

    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid SeriesId = Guid.NewGuid();
    private static readonly Guid TrainerId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _enrollmentRepo = new Mock<IEnrollmentRepository>();
        _enrollmentFormRepo = new Mock<IEnrollmentFormRepository>();
        _lessonSeriesRepo = new Mock<ILessonSerieRepository>();
        _userLookup = new Mock<IUserLookupService>();
        _emailService = new Mock<IEmailService>();
        _mapper = new ApplicationMapper();
        _logger = new Mock<ILogger<EnrollmentService>>();

        _service = new EnrollmentService(
            _enrollmentRepo.Object,
            _enrollmentFormRepo.Object,
            _lessonSeriesRepo.Object,
            _userLookup.Object,
            _emailService.Object,
            _mapper,
            _logger.Object);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static LessonSerie BuildActiveSeries() => new()
    {
        Id = SeriesId,
        OrganizationId = OrgId,
        Name = "Beginners A",
        Level = LessonLevel.Beginner,
        Price = 120m,
        StartDate = DateOnly.FromDateTime(DateTime.Today),
        EndDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(3)),
        IsActive = true,
        Lessons = new List<Lesson>
        {
            new()
            {
                Id = Guid.NewGuid(),
                OrganizationId = OrgId,
                TrainerId = TrainerId,
                Date = DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
                StartTime = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
                EndTime = TimeOnly.FromTimeSpan(TimeSpan.FromHours(11)),
                CourtName = "Baan 1",
            }
        },
    };

    private static EnrollmentForm BuildFormWithFields() => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = OrgId,
        LessonSerieId = SeriesId,
        Fields = new List<FormField>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Label = "Geboortedatum",
                Type = FormFieldType.Text,
                IsRequired = true,
                Order = 0,
            },
        },
    };

    // ── GetPublicLessonSerieAsync ───────────────────────────────────────────

    [Test]
    public async Task GetPublicLessonSerie_ReturnsDto_WhenFound()
    {
        var series = BuildActiveSeries();
        _lessonSeriesRepo
            .Setup(r => r.GetByIdPublicAsync(SeriesId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);
        _enrollmentRepo
            .Setup(r => r.CountActiveBySeriesAsync(SeriesId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var result = await _service.GetPublicLessonSerieAsync(SeriesId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Beginners A");
        result.Value.EnrollmentCount.Should().Be(5);
    }

    [Test]
    public async Task GetPublicLessonSerie_ReturnsNotFound_WhenMissing()
    {
        _lessonSeriesRepo
            .Setup(r => r.GetByIdPublicAsync(SeriesId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LessonSerie?)null);

        var result = await _service.GetPublicLessonSerieAsync(SeriesId);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.NotFound);
    }

    // ── GetEnrollmentFormAsync ───────────────────────────────────────────────

    [Test]
    public async Task GetEnrollmentForm_ReturnsNull_WhenNoForm()
    {
        _enrollmentFormRepo
            .Setup(r => r.GetBySeriesIdReadOnlyAsync(SeriesId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EnrollmentForm?)null);

        var result = await _service.GetEnrollmentFormAsync(SeriesId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Test]
    public async Task GetEnrollmentForm_ReturnsDto_WhenExists()
    {
        var form = BuildFormWithFields();
        _enrollmentFormRepo
            .Setup(r => r.GetBySeriesIdReadOnlyAsync(SeriesId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(form);

        var result = await _service.GetEnrollmentFormAsync(SeriesId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Fields.Should().HaveCount(1);
        result.Value.Fields[0].Label.Should().Be("Geboortedatum");
    }

    // ── GetSeriesEnrollmentsAsync ────────────────────────────────────────────

    [Test]
    public async Task GetSeriesEnrollments_ReturnsList()
    {
        List<Enrollment> enrollments = new()
        {
            new()
            {
                Id = Guid.NewGuid(),
                OrganizationId = OrgId,
                LessonSerieId = SeriesId,
                StudentName = "Piet Janssen",
                StudentEmail = "piet@test.be",
                Status = EnrollmentStatus.Confirmed,
                EnrolledAt = DateTime.UtcNow,
                FormResponses = new List<FormResponse>(),
            },
        };

        _enrollmentRepo
            .Setup(r => r.GetBySeriesAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(enrollments);

        var result =
            await _service.GetSeriesEnrollmentsAsync(SeriesId, OrgId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value![0].StudentName.Should().Be("Piet Janssen");
    }

    // ── SaveFormAsync ────────────────────────────────────────────────────────

    [Test]
    public async Task SaveForm_ReturnsNotFound_WhenSeriesMissing()
    {
        _lessonSeriesRepo
            .Setup(r => r.ExistsAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        SaveEnrollmentFormRequest request = new() { Fields = new() };

        var result = await _service.SaveFormAsync(SeriesId, OrgId, request);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.NotFound);
    }

    [Test]
    public async Task SaveForm_CreatesNewForm_WhenNoneExists()
    {
        _lessonSeriesRepo
            .Setup(r => r.ExistsAsync(SeriesId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _enrollmentFormRepo
            .Setup(r => r.GetBySeriesIdWithFieldsAsync(SeriesId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EnrollmentForm?)null);

        SaveEnrollmentFormRequest request = new()
        {
            Fields = new()
            {
                new() { Label = "Naam ouder", Type = (int)FormFieldType.Text, IsRequired = true },
            },
        };

        var result = await _service.SaveFormAsync(SeriesId, OrgId, request);

        result.IsSuccess.Should().BeTrue();
        _enrollmentFormRepo.Verify(
            r => r.AddAsync(It.IsAny<EnrollmentForm>(), It.IsAny<CancellationToken>()), Times.Once);
        _enrollmentFormRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── SubmitEnrollmentAsync ────────────────────────────────────────────────

    [Test]
    public async Task SubmitEnrollment_ReturnsNotFound_WhenSeriesMissing()
    {
        _lessonSeriesRepo
            .Setup(r => r.GetByIdPublicAsync(SeriesId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LessonSerie?)null);

        SubmitEnrollmentRequest request = new()
        {
            StudentName = "Test",
            StudentEmail = "test@test.be",
        };

        var result = await _service.SubmitEnrollmentAsync(SeriesId, request);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.NotFound);
    }

    [Test]
    public async Task SubmitEnrollment_ReturnsDuplicate_WhenAlreadyEnrolled()
    {
        var series = BuildActiveSeries();
        _lessonSeriesRepo
            .Setup(r => r.GetByIdPublicAsync(SeriesId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);
        _enrollmentFormRepo
            .Setup(r => r.GetBySeriesIdReadOnlyAsync(SeriesId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EnrollmentForm?)null);
        _enrollmentRepo
            .Setup(r => r.IsDuplicateAsync(SeriesId, "piet@test.be", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        SubmitEnrollmentRequest request = new()
        {
            StudentName = "Piet",
            StudentEmail = "piet@test.be",
        };

        var result = await _service.SubmitEnrollmentAsync(SeriesId, request);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.Conflict);
    }

    [Test]
    public async Task SubmitEnrollment_Succeeds_WhenValid()
    {
        var series = BuildActiveSeries();
        _lessonSeriesRepo
            .Setup(r => r.GetByIdPublicAsync(SeriesId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);
        _enrollmentFormRepo
            .Setup(r => r.GetBySeriesIdReadOnlyAsync(SeriesId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EnrollmentForm?)null);
        _enrollmentRepo
            .Setup(r => r.IsDuplicateAsync(SeriesId, "anna@test.be", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _userLookup
            .Setup(u => u.GetUserInfoByIdAsync(TrainerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(("Jan Peeters", "jan@coach.be"));

        SubmitEnrollmentRequest request = new()
        {
            StudentName = "Anna",
            StudentEmail = "anna@test.be",
        };

        var result = await _service.SubmitEnrollmentAsync(SeriesId, request);

        result.IsSuccess.Should().BeTrue();
        _enrollmentRepo.Verify(
            r => r.AddAsync(It.Is<Enrollment>(e => e.StudentName == "Anna"), It.IsAny<CancellationToken>()),
            Times.Once);
        _enrollmentRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task SubmitEnrollment_FailsValidation_WhenRequiredFieldMissing()
    {
        var series = BuildActiveSeries();
        var form = BuildFormWithFields();

        _lessonSeriesRepo
            .Setup(r => r.GetByIdPublicAsync(SeriesId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(series);
        _enrollmentFormRepo
            .Setup(r => r.GetBySeriesIdReadOnlyAsync(SeriesId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(form);

        SubmitEnrollmentRequest request = new()
        {
            StudentName = "Anna",
            StudentEmail = "anna@test.be",
            Responses = new(), // Missing required field response
        };

        var result = await _service.SubmitEnrollmentAsync(SeriesId, request);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.Validation);
    }
}
