using CoachOS.Application.Abstractions;
using CoachOS.Application.Export;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Services;

[TestFixture]
public class CampExportServiceTests
{
    private Mock<ICampRepository> _campRepo = null!;
    private Mock<ICampEnrollmentRepository> _enrollmentRepo = null!;
    private Mock<IPaymentRepository> _paymentRepo = null!;
    private Mock<IUserLookupService> _userLookup = null!;
    private Mock<ICampWorkbookBuilder> _builder = null!;
    private CampExportService _service = null!;
    private CampExportModel? _captured;

    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid CampId = Guid.NewGuid();
    private static readonly Guid TrainerId = Guid.NewGuid();
    private static readonly Guid EnrollmentId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _campRepo = new Mock<ICampRepository>();
        _enrollmentRepo = new Mock<ICampEnrollmentRepository>();
        _paymentRepo = new Mock<IPaymentRepository>();
        _userLookup = new Mock<IUserLookupService>();
        _builder = new Mock<ICampWorkbookBuilder>();
        _captured = null;

        _userLookup
            .Setup(u => u.GetUserNamesByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, string> { [TrainerId] = "Jan Janssen" });
        _paymentRepo
            .Setup(p => p.GetLatestMethodAndStatusByCampEnrollmentIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, (PaymentMethod? Method, PaymentStatus Status)>
            {
                [EnrollmentId] = (PaymentMethod.Cash, PaymentStatus.Paid),
            });
        _builder.Setup(b => b.Build(It.IsAny<CampExportModel>()))
            .Callback<CampExportModel>(m => _captured = m)
            .Returns([1, 2, 3]);

        _service = new CampExportService(
            _campRepo.Object, _enrollmentRepo.Object, _paymentRepo.Object,
            _userLookup.Object, _builder.Object, TimeProvider.System);
    }

    [Test]
    public async Task ExportCampAsync_CampNotFound_ReturnsNotFound()
    {
        _campRepo.Setup(r => r.GetByIdWithDetailsAsync(CampId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Camp?)null);

        var result = await _service.ExportCampAsync(CampId, OrgId);

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("not_found");
        _builder.Verify(b => b.Build(It.IsAny<CampExportModel>()), Times.Never);
    }

    [Test]
    public async Task ExportCampAsync_MapsDaysTrainersEnrollmentsAndPayment()
    {
        Camp camp = new()
        {
            Id = CampId,
            OrganizationId = OrgId,
            Name = "Zomerkamp",
            StartDate = new DateOnly(2026, 7, 6),
            EndDate = new DateOnly(2026, 7, 7),
            Days =
            [
                new CampDay
                {
                    Date = new DateOnly(2026, 7, 7),
                    StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(16, 0),
                    TrainerAssignments = [new CampDayTrainer { TrainerId = TrainerId, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(12, 0) }],
                },
                new CampDay { Date = new DateOnly(2026, 7, 6), StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(16, 0) },
            ],
        };
        _campRepo.Setup(r => r.GetByIdWithDetailsAsync(CampId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(camp);
        _enrollmentRepo.Setup(r => r.GetByCampWithResponsesAsync(CampId, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new CampEnrollment
                {
                    Id = EnrollmentId, ParticipantName = "Emma", ParticipantEmail = "emma@test.com",
                    Status = EnrollmentStatus.Confirmed, EnrolledAt = DateTime.UtcNow,
                },
            ]);

        var result = await _service.ExportCampAsync(CampId, OrgId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.FileName.Should().StartWith("Zomerkamp-kamp-").And.EndWith(".xlsx");
        _captured!.Days.Select(d => d.Date).Should().BeInAscendingOrder();
        _captured.Days[1].DayName.Should().Be("Dinsdag");
        _captured.Days[1].Trainers.Should().ContainSingle(t => t.Name == "Jan Janssen");
        CampEnrollmentRow row = _captured.Enrollments.Should().ContainSingle().Subject;
        row.Status.Should().Be("Bevestigd");
        row.PaymentStatus.Should().Be("Betaald");
        row.PaymentMethod.Should().Be("Cash");
        row.EnrollmentType.Should().Be("Individueel");
    }
}
