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

/// <summary>
/// Gedeeld contactadres: een ouder of vriend draagt de communicatie voor meerdere
/// deelnemers. Verifieert dat ContactEmail correct wordt afgeleid, dat dezelfde persoon
/// niet twee keer kan, en dat er per contactadres maar één bevestigingsmail uitgaat.
/// </summary>
[TestFixture]
public class SharedContactEmailTests
{
    private Mock<IEnrollmentRepository> _enrollmentRepo = null!;
    private Mock<IEnrollmentFormRepository> _enrollmentFormRepo = null!;
    private Mock<ILessonSerieRepository> _lessonSeriesRepo = null!;
    private Mock<IEnrollmentGroupRepository> _enrollmentGroupRepo = null!;
    private Mock<ITimeSlotPreferenceRepository> _timeSlotPreferenceRepo = null!;
    private Mock<IOrganizationSettingsRepository> _orgSettingsRepo = null!;
    private Mock<IUserLookupService> _userLookup = null!;
    private Mock<IEmailService> _emailService = null!;
    private ApplicationMapper _mapper = null!;
    private Mock<ILogger<EnrollmentService>> _logger = null!;
    private EnrollmentService _service = null!;

    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid SeriesId = Guid.NewGuid();

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
        _emailService = new Mock<IEmailService>();
        _mapper = new ApplicationMapper();
        _logger = new Mock<ILogger<EnrollmentService>>();

        // Reeks bestaat met ruime capaciteit, geen formulier, standaard org-instellingen.
        _lessonSeriesRepo
            .Setup(r => r.GetByIdPublicAsync(SeriesId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LessonSerie
            {
                Id = SeriesId,
                OrganizationId = OrgId,
                Name = "Beginners A",
                Price = 120m,
                IsActive = true,
                MaxRegistrations = 20,
                StartDate = DateOnly.FromDateTime(DateTime.Today),
                EndDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(3)),
                RegistrationDeadline = DateTime.UtcNow.AddMonths(1),
            });
        _enrollmentFormRepo
            .Setup(r => r.GetBySeriesIdReadOnlyAsync(SeriesId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EnrollmentForm?)null);
        _orgSettingsRepo
            .Setup(r => r.GetByOrganizationReadOnlyAsync(OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrganizationSettings?)null);
        _enrollmentGroupRepo
            .Setup(r => r.CountBySeriesAsync(SeriesId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _service = new EnrollmentService(
            _enrollmentRepo.Object,
            _enrollmentFormRepo.Object,
            _lessonSeriesRepo.Object,
            _enrollmentGroupRepo.Object,
            _timeSlotPreferenceRepo.Object,
            _orgSettingsRepo.Object,
            _userLookup.Object,
            _emailService.Object,
            _mapper,
            _logger.Object);
    }

    [Test]
    public async Task Group_Members_Without_Own_Email_Inherit_The_Leader_Address()
    {
        List<Enrollment> added = CaptureAddedEnrollments();

        SubmitEnrollmentRequest request = GroupRequest(
            leaderEmail: "ouder@example.com",
            members:
            [
                ("Lotte Peeters", null, "2015-03-04"),
                ("Sofie Peeters", null, "2017-06-11"),
            ]);

        Result<Guid> result = await _service.SubmitEnrollmentAsync(SeriesId, request);

        result.IsSuccess.Should().BeTrue();
        added.Should().HaveCount(3);
        added.Should().OnlyContain(e => e.ContactEmail == "ouder@example.com");
        added.Where(e => e.StudentName != "Els Peeters")
            .Should().OnlyContain(e => e.StudentEmail == null);
    }

    [Test]
    public async Task Group_Member_With_Own_Email_Keeps_It_As_Contact()
    {
        List<Enrollment> added = CaptureAddedEnrollments();

        SubmitEnrollmentRequest request = GroupRequest(
            leaderEmail: "els@example.com",
            members: [("Jan Peeters", "jan@example.com", "1990-02-02")]);

        await _service.SubmitEnrollmentAsync(SeriesId, request);

        Enrollment member = added.Single(e => e.StudentName == "Jan Peeters");
        member.ContactEmail.Should().Be("jan@example.com");
        member.StudentEmail.Should().Be("jan@example.com");
    }

    [Test]
    public async Task Same_Participant_Already_Enrolled_Is_Rejected()
    {
        _enrollmentRepo
            .Setup(r => r.IsDuplicateParticipantAsync(
                SeriesId, "ouder@example.com", "Lotte Peeters",
                new DateOnly(2015, 3, 4), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        SubmitEnrollmentRequest request = GroupRequest(
            leaderEmail: "ouder@example.com",
            members: [("Lotte Peeters", null, "2015-03-04")]);

        Result<Guid> result = await _service.SubmitEnrollmentAsync(SeriesId, request);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ErrorCodes.Conflict);
        _enrollmentRepo.Verify(r => r.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Confirmation_Email_Is_Sent_Once_Per_Contact_Address()
    {
        SubmitEnrollmentRequest request = GroupRequest(
            leaderEmail: "ouder@example.com",
            members:
            [
                ("Lotte Peeters", null, "2015-03-04"),
                ("Sofie Peeters", null, "2017-06-11"),
            ]);

        await _service.SubmitEnrollmentAsync(SeriesId, request);

        _emailService.Verify(s => s.SendEnrollmentConfirmationAsync(
            "ouder@example.com", It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void No_Sender_Uses_StudentEmail_Anymore()
    {
        string[] senderFiles =
        [
            "Payments/PaymentService.cs",
            "LessonSerie/LessonSerieService.cs",
            "LessonReschedule/LessonRescheduleService.cs",
            "Planning/ConfirmationOrchestrationService.cs",
        ];

        string root = Path.Combine(TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "CoachOS.Application");

        foreach (string file in senderFiles)
        {
            string source = File.ReadAllText(Path.Combine(root, file));
            source.Should().NotContain(".StudentEmail,",
                because: $"{file} moet naar ContactEmail sturen, niet naar het adres van de deelnemer");
        }
    }

    private List<Enrollment> CaptureAddedEnrollments()
    {
        List<Enrollment> added = [];
        _enrollmentRepo
            .Setup(r => r.AddAsync(It.IsAny<Enrollment>(), It.IsAny<CancellationToken>()))
            .Callback<Enrollment, CancellationToken>((e, _) => added.Add(e))
            .Returns(Task.CompletedTask);
        return added;
    }

    private static SubmitEnrollmentRequest GroupRequest(
        string leaderEmail,
        List<(string Name, string? Email, string Dob)> members) => new()
    {
        StudentName = "Els Peeters",
        StudentEmail = leaderEmail,
        DateOfBirth = "1985-01-01",
        EnrollmentType = "group",
        GroupMembers = members
            .Select(m => new GroupMemberDto
            {
                StudentName = m.Name,
                StudentEmail = m.Email,
                DateOfBirth = m.Dob,
            })
            .ToList(),
    };
}
