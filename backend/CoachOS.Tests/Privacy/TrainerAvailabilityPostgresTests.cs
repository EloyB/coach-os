using AwesomeAssertions;
using CoachOS.Application.Abstractions;
using CoachOS.Application.Mappings;
using CoachOS.Application.TrainerAvailabilities;
using CoachOS.Application.TrainerAvailabilities.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using CoachOS.Infrastructure.Persistence;
using CoachOS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Privacy;

/// <summary>
/// Opt-in (zet <c>COACHOS_TEST_PG</c>): gelijktijdige overlappende beschikbaarheden van dezelfde trainer
/// mogen niet allemaal slagen (#381). Elke aanroep krijgt een eigen DbContext, zoals een eigen request.
/// </summary>
[TestFixture]
public class TrainerAvailabilityPostgresTests : PlayerErasurePostgresBase
{
    [Test]
    public async Task ConcurrentOverlappingCreates_OnlyOneSucceeds()
    {
        Guid orgId = Guid.NewGuid();
        Guid trainerId = Guid.NewGuid();
        await using (ApplicationDbContext seed = Db())
        {
            seed.Organizations.Add(new Organization { Id = orgId, Name = "Club", Email = $"c-{orgId:N}@example.test" });
            await seed.SaveChangesAsync();
        }

        Mock<IUserLookupService> userLookup = new();
        userLookup.Setup(u => u.IsActiveTrainerAsync(trainerId, orgId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        Barrier barrier = new(8);

        Result<Guid>[] results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(async () =>
        {
            await using ApplicationDbContext db = Db();
            TrainerAvailabilityService service = new(
                new TrainerAvailabilityRepository(db), Mock.Of<ITennisClubRepository>(), new UnitOfWork(db), userLookup.Object, new ApplicationMapper());
            barrier.SignalAndWait();
            return await service.CreateAsync(orgId, new CreateTrainerAvailabilityRequest(trainerId, null, 0, "09:00", "12:00"));
        })));

        results.Count(r => r.IsSuccess).Should().Be(1);
        results.Where(r => !r.IsSuccess).Should().OnlyContain(r => r.Errors.Any(e => e.Code == ErrorCodes.Conflict));
        await using ApplicationDbContext verify = Db();
        (await verify.TrainerAvailabilities.CountAsync(a => a.TrainerId == trainerId && a.IsActive)).Should().Be(1);
    }
}
