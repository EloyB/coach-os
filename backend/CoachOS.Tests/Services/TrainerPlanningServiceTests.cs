using CoachOS.Application.Planning;
using CoachOS.Application.Planning.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Services;

[TestFixture]
public class TrainerPlanningServiceTests
{
    private Mock<ILessonSerieRepository> _seriesRepo = null!;
    private Mock<IPlanningService> _planningService = null!;
    private TrainerPlanningService _service = null!;

    private static readonly Guid OrgId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _seriesRepo = new Mock<ILessonSerieRepository>();
        _planningService = new Mock<IPlanningService>();
        _service = new TrainerPlanningService(_seriesRepo.Object, _planningService.Object);
    }

    [Test]
    public async Task GetAllAsync_ReturnsEveryOrganizationSeriesIncludingSeriesWithoutTrainerSlots()
    {
        var first = new LessonSerie { Id = Guid.NewGuid(), OrganizationId = OrgId, Name = "Avondreeks" };
        var second = new LessonSerie { Id = Guid.NewGuid(), OrganizationId = OrgId, Name = "Jeugdreeks" };
        _seriesRepo.Setup(r => r.GetByOrganizationAsync(OrgId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { first, second });
        _planningService.Setup(s => s.GetPlanningOverviewAsync(It.IsAny<Guid>(), OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, Guid _, CancellationToken _) => Result<PlanningOverviewDto>.Ok(
                new PlanningOverviewDto { PlanningStatus = "Planning" }));

        var result = await _service.GetAllAsync(OrgId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Select(x => x.LessonSerieName).Should().BeEquivalentTo("Avondreeks", "Jeugdreeks");
        _planningService.Verify(s => s.GetPlanningOverviewAsync(first.Id, OrgId, It.IsAny<CancellationToken>()), Times.Once);
        _planningService.Verify(s => s.GetPlanningOverviewAsync(second.Id, OrgId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetAllAsync_ProjectsOnlyStudentNamesAndNeverContactDetails()
    {
        var series = new LessonSerie { Id = Guid.NewGuid(), OrganizationId = OrgId, Name = "Avondreeks" };
        _seriesRepo.Setup(r => r.GetByOrganizationAsync(OrgId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { series });
        _planningService.Setup(s => s.GetPlanningOverviewAsync(series.Id, OrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PlanningOverviewDto>.Ok(new PlanningOverviewDto
            {
                Enrollments =
                [
                    new PlanningEnrollmentDto
                    {
                        Id = Guid.NewGuid(),
                        StudentName = "Alice Trainer",
                        StudentEmail = "alice@example.com",
                        StudentPhone = "+3212345678",
                    },
                ],
            }));

        var result = await _service.GetAllAsync(OrgId);

        result.IsSuccess.Should().BeTrue();
        var enrollment = result.Value!.Single().Enrollments.Single();
        enrollment.Should().BeEquivalentTo(new TrainerPlanningEnrollmentDto
        {
            Id = enrollment.Id,
            StudentName = "Alice Trainer",
        });
    }
}
