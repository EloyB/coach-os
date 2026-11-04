using CoachOS.Application.Abstractions;
using CoachOS.Application.Dashboard;
using CoachOS.Application.Dashboard.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Dashboard;

[TestFixture]
public class InboxTests
{
    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 10, 19, 10, 0, 0, TimeSpan.Zero);

    private sealed class FixedTime(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    [Test]
    public async Task GetInbox_ReportsTotalCount_EvenWhenLimitShortensTheList()
    {
        List<AssignmentConfirmationToken> tokens = Enumerable.Range(0, 12).Select(i => new AssignmentConfirmationToken
        {
            ExpiresAt = Now.UtcDateTime.AddHours(30),
            CreatedAt = Now.UtcDateTime.AddMinutes(-i),
            Enrollment = new Enrollment { StudentName = $"Speler {i}" },
            ScheduleAssignment = new ScheduleAssignment { LessonSerieId = Guid.NewGuid() },
        }).ToList();

        Mock<IAssignmentConfirmationTokenRepository> tokenRepo = new();
        tokenRepo.Setup(r => r.GetPendingByOrganizationAsync(OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokens);
        Mock<ILessonSerieRepository> seriesRepo = new();
        seriesRepo.Setup(r => r.GetByOrganizationAsync(
                OrgId, null, It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LessonSerie>());

        DashboardService sut = new(
            seriesRepo.Object, Mock.Of<ILessonRepository>(), Mock.Of<IEnrollmentRepository>(),
            Mock.Of<ITennisClubRepository>(), Mock.Of<IUserLookupService>(), tokenRepo.Object,
            Mock.Of<IScheduleAssignmentRepository>(), Mock.Of<ILessonInvitationRepository>(),
            Mock.Of<ICampRepository>(), new FixedTime(Now));

        Result<InboxDto> result = await sut.GetInboxAsync(OrgId, 5);

        result.Value!.Items.Should().HaveCount(5);
        result.Value.TotalCount.Should().Be(12);
    }
}
