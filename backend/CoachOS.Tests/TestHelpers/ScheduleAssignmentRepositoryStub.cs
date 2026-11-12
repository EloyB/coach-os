using CoachOS.Domain.Interfaces;
using Moq;

namespace CoachOS.Tests.TestHelpers;

/// <summary>
/// Planning-repository voor tests die niet over toewijzingen gaan: geen enkele
/// deelnemer wacht op bevestiging. Moq zou voor de HashSet anders null teruggeven.
/// </summary>
public static class ScheduleAssignmentRepositoryStub
{
    public static IScheduleAssignmentRepository NoneAwaitingConfirmation()
    {
        var repo = new Mock<IScheduleAssignmentRepository>();
        repo.Setup(r => r.GetAwaitingConfirmationParticipantIdsAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        return repo.Object;
    }
}
