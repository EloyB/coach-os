using System.Security.Cryptography;
using System.Text;
using CoachOS.Application.StandaloneLessons;
using CoachOS.Application.StandaloneLessons.DTOs;
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
public class InvitationPublicServiceTests
{
    private Mock<ILessonInvitationRepository> _invitationRepo = null!;
    private Mock<IUserLookupService> _userLookup = null!;
    private InvitationPublicService _service = null!;

    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid TrainerId = Guid.NewGuid();
    private const string RawToken = "test-raw-token-not-really-32-bytes-but-it-doesnt-matter-for-tests";

    private static string TokenHash =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(RawToken))).ToLowerInvariant();

    [SetUp]
    public void SetUp()
    {
        _invitationRepo = new Mock<ILessonInvitationRepository>();
        _userLookup = new Mock<IUserLookupService>();
        _service = new InvitationPublicService(
            _invitationRepo.Object,
            _userLookup.Object,
            NullLogger<InvitationPublicService>.Instance);

        _userLookup
            .Setup(u => u.GetUserNameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Sara Trainer");
    }

    private static (LessonInvitation Invitation, Lesson Lesson) BuildPair(
        LessonInvitationStatus status = LessonInvitationStatus.Pending,
        bool cancelled = false,
        int maxStudents = 0)
    {
        Lesson lesson = new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            LessonSerieId = null,
            TrainerId = TrainerId,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
            CourtName = "Baan 1",
            Level = LessonLevel.Beginner,
            MaxStudents = maxStudents,
            IsCancelled = cancelled,
        };
        LessonInvitation invitation = new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            LessonId = lesson.Id,
            Email = "alice@test.com",
            TokenHash = TokenHash,
            Status = status,
            Lesson = lesson,
        };
        return (invitation, lesson);
    }

    // ── GetByTokenAsync ───────────────────────────────────────────────────────

    [Test]
    public async Task GetByTokenAsync_ReturnsDto_ForValidToken()
    {
        (LessonInvitation invitation, _) = BuildPair();
        _invitationRepo
            .Setup(r => r.GetByTokenHashAsync(TokenHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);

        Result<PublicInvitationDto> result =
            await _service.GetByTokenAsync(RawToken, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Email.Should().Be("alice@test.com");
        result.Value.TrainerName.Should().Be("Sara Trainer");
        result.Value.Status.Should().Be((int)LessonInvitationStatus.Pending);
    }

    [Test]
    public async Task GetByTokenAsync_ReturnsNotFound_ForUnknownToken()
    {
        _invitationRepo
            .Setup(r => r.GetByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LessonInvitation?)null);

        Result<PublicInvitationDto> result =
            await _service.GetByTokenAsync(RawToken, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.NotFound);
    }

    [Test]
    public async Task GetByTokenAsync_ReturnsNotFound_ForEmptyToken()
    {
        Result<PublicInvitationDto> result =
            await _service.GetByTokenAsync("", CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.NotFound);
        // Token is leeg → mag niet eens aan de DB worden gevraagd.
        _invitationRepo.Verify(r => r.GetByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── AcceptAsync ───────────────────────────────────────────────────────────

    [Test]
    public async Task AcceptAsync_ClaimsResponse_AndReturnsAccepted()
    {
        (LessonInvitation invitation, Lesson lesson) = BuildPair();
        // Eerste lookup geeft de pending entity.
        _invitationRepo
            .SetupSequence(r => r.GetByTokenHashAsync(TokenHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation)
            .ReturnsAsync(new LessonInvitation
            {
                Id = invitation.Id,
                OrganizationId = OrgId,
                LessonId = lesson.Id,
                Email = invitation.Email,
                Status = LessonInvitationStatus.Accepted,
                RespondedAt = DateTime.UtcNow,
                Lesson = lesson,
                TokenHash = TokenHash,
            });
        _invitationRepo
            .Setup(r => r.TryClaimResponseAsync(
                invitation.Id, LessonInvitationStatus.Accepted,
                It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Result<PublicInvitationDto> result =
            await _service.AcceptAsync(RawToken, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be((int)LessonInvitationStatus.Accepted);
        _invitationRepo.Verify(r => r.TryClaimResponseAsync(
            invitation.Id, LessonInvitationStatus.Accepted,
            It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task AcceptAsync_RejectsCancelledLesson()
    {
        (LessonInvitation invitation, _) = BuildPair(cancelled: true);
        _invitationRepo
            .Setup(r => r.GetByTokenHashAsync(TokenHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);

        Result<PublicInvitationDto> result =
            await _service.AcceptAsync(RawToken, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.Validation);
        _invitationRepo.Verify(r => r.TryClaimResponseAsync(
            It.IsAny<Guid>(), It.IsAny<LessonInvitationStatus>(),
            It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task AcceptAsync_RejectsAlreadyAnswered()
    {
        (LessonInvitation invitation, _) = BuildPair(status: LessonInvitationStatus.Accepted);
        _invitationRepo
            .Setup(r => r.GetByTokenHashAsync(TokenHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);

        Result<PublicInvitationDto> result =
            await _service.AcceptAsync(RawToken, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.Conflict);
    }

    [Test]
    public async Task AcceptAsync_RejectsWhenAtCapacity()
    {
        (LessonInvitation invitation, Lesson lesson) = BuildPair(maxStudents: 2);
        _invitationRepo
            .Setup(r => r.GetByTokenHashAsync(TokenHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);
        // 2 reeds Accepted → volzet.
        _invitationRepo
            .Setup(r => r.GetByLessonAsync(lesson.Id, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LessonInvitation>
            {
                new() { Status = LessonInvitationStatus.Accepted, Email = "x@x.com" },
                new() { Status = LessonInvitationStatus.Accepted, Email = "y@y.com" },
                new() { Status = LessonInvitationStatus.Pending, Email = "alice@test.com" },
            });

        Result<PublicInvitationDto> result =
            await _service.AcceptAsync(RawToken, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.Conflict);
    }

    [Test]
    public async Task AcceptAsync_ReturnsConflict_WhenClaimFails()
    {
        // Concurrent request flipte de status net voor onze atomic claim.
        (LessonInvitation invitation, _) = BuildPair();
        _invitationRepo
            .Setup(r => r.GetByTokenHashAsync(TokenHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);
        _invitationRepo
            .Setup(r => r.TryClaimResponseAsync(
                It.IsAny<Guid>(), It.IsAny<LessonInvitationStatus>(),
                It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Result<PublicInvitationDto> result =
            await _service.AcceptAsync(RawToken, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.Conflict);
    }

    // ── DeclineAsync ──────────────────────────────────────────────────────────

    [Test]
    public async Task DeclineAsync_ClaimsResponse_AndReturnsDeclined()
    {
        (LessonInvitation invitation, Lesson lesson) = BuildPair();
        _invitationRepo
            .SetupSequence(r => r.GetByTokenHashAsync(TokenHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation)
            .ReturnsAsync(new LessonInvitation
            {
                Id = invitation.Id,
                OrganizationId = OrgId,
                LessonId = lesson.Id,
                Email = invitation.Email,
                Status = LessonInvitationStatus.Declined,
                RespondedAt = DateTime.UtcNow,
                Lesson = lesson,
                TokenHash = TokenHash,
            });
        _invitationRepo
            .Setup(r => r.TryClaimResponseAsync(
                invitation.Id, LessonInvitationStatus.Declined,
                It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Result<PublicInvitationDto> result =
            await _service.DeclineAsync(RawToken, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be((int)LessonInvitationStatus.Declined);
    }
}
