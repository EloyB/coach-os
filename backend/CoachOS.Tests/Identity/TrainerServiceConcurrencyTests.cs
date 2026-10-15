using CoachOS.Application.Abstractions;
using CoachOS.Application.Auth;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using CoachOS.Infrastructure.Identity;
using CoachOS.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using Npgsql;
using NUnit.Framework;

namespace CoachOS.Tests.Identity;

/// <summary>
/// Bewijst dat de "laatste actieve admin"-guard ook onder gelijktijdige requests standhoudt.
/// Dit vereist een echte Postgres (advisory lock + transacties); InMemory kan dat niet testen.
/// Opt-in: zet COACHOS_TEST_PG op een connection string met rechten om databases aan te maken,
/// bv. "Host=localhost;Port=5432;Database=postgres;Username=coachos;Password=coachos".
/// Zonder die variabele worden de tests overgeslagen.
/// </summary>
[TestFixture]
[NonParallelizable]
public class TrainerServiceConcurrencyTests
{
    private const int Rounds = 25;

    private string? _adminConnectionString;
    private string _databaseName = null!;
    private DbContextOptions<ApplicationDbContext> _options = null!;

    private sealed class NoTenantContext : ITenantContext
    {
        public Guid OrganizationId => Guid.Empty;
        public Guid UserId => Guid.Empty;
        public bool IsAuthenticated => false;
    }

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _adminConnectionString = Environment.GetEnvironmentVariable("COACHOS_TEST_PG");
        if (string.IsNullOrWhiteSpace(_adminConnectionString))
            Assert.Ignore("COACHOS_TEST_PG niet gezet: concurrency-tests tegen Postgres overgeslagen.");

        _databaseName = $"coachos_conc_{Guid.NewGuid():N}";
        await using (NpgsqlConnection admin = new(_adminConnectionString))
        {
            await admin.OpenAsync();
            await using NpgsqlCommand create = new($"CREATE DATABASE \"{_databaseName}\"", admin);
            await create.ExecuteNonQueryAsync();
        }

        NpgsqlConnectionStringBuilder builder = new(_adminConnectionString) { Database = _databaseName };
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(builder.ConnectionString)
            .Options;

        await using ApplicationDbContext db = new(_options, new NoTenantContext());
        await db.Database.EnsureCreatedAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        if (string.IsNullOrWhiteSpace(_adminConnectionString) || _databaseName is null)
            return;

        NpgsqlConnection.ClearAllPools();
        await using NpgsqlConnection admin = new(_adminConnectionString);
        await admin.OpenAsync();
        await using NpgsqlCommand drop = new($"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE)", admin);
        await drop.ExecuteNonQueryAsync();
    }

    private TrainerService CreateService(ApplicationDbContext db)
    {
        Mock<UserManager<ApplicationUser>> userManager = new(
            Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);
        return new TrainerService(
            userManager.Object, Mock.Of<IEmailService>(), db, Mock.Of<IAuthService>(),
            Mock.Of<IOrganizationSettingsRepository>(), TimeProvider.System);
    }

    private async Task<(Guid orgId, Guid adminA, Guid adminB)> SeedOrgWithTwoAdminsAsync()
    {
        Guid orgId = Guid.NewGuid();
        await using ApplicationDbContext db = new(_options, new NoTenantContext());

        db.Organizations.Add(new Organization
        {
            Id = orgId,
            Name = $"Org {orgId:N}",
            Email = $"{orgId:N}@test.be",
        });

        List<Guid> ids = [];
        for (int i = 0; i < 2; i++)
        {
            ApplicationUser user = new()
            {
                Id = Guid.NewGuid(),
                UserName = $"{Guid.NewGuid():N}@test.be",
                Email = $"{Guid.NewGuid():N}@test.be",
                FirstName = "Admin",
                LastName = $"{i}",
                IsActive = true,
            };
            db.Users.Add(user);
            db.OrganizationMemberships.Add(new OrganizationMembership
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                OrganizationId = orgId,
                Role = UserRole.Admin,
                IsActive = true,
                JoinedAt = DateTime.UtcNow,
            });
            ids.Add(user.Id);
        }

        await db.SaveChangesAsync();
        return (orgId, ids[0], ids[1]);
    }

    private async Task<int> CountActiveAdminsAsync(Guid orgId)
    {
        await using ApplicationDbContext db = new(_options, new NoTenantContext());
        return await db.OrganizationMemberships
            .CountAsync(m => m.OrganizationId == orgId && m.Role == UserRole.Admin && m.IsActive);
    }

    private static async Task<Result[]> RunConcurrentlyAsync(params Func<Task<Result>>[] actions)
    {
        TaskCompletionSource start = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<Result>[] tasks = actions
            .Select(async action =>
            {
                await start.Task;
                return await action();
            })
            .ToArray();
        start.SetResult();
        return await Task.WhenAll(tasks);
    }

    [Test]
    public async Task SetRole_TwoAdminsDemoteEachOtherConcurrently_OneAdminRemains()
    {
        for (int round = 0; round < Rounds; round++)
        {
            (Guid orgId, Guid a, Guid b) = await SeedOrgWithTwoAdminsAsync();
            await using ApplicationDbContext db1 = new(_options, new NoTenantContext());
            await using ApplicationDbContext db2 = new(_options, new NoTenantContext());
            TrainerService s1 = CreateService(db1);
            TrainerService s2 = CreateService(db2);

            Result[] results = await RunConcurrentlyAsync(
                () => s1.SetRoleAsync(a, orgId, UserRole.Trainer),
                () => s2.SetRoleAsync(b, orgId, UserRole.Trainer));

            results.Count(r => r.IsSuccess).Should().Be(1, $"ronde {round}: precies één demotie mag slagen");
            (await CountActiveAdminsAsync(orgId)).Should().Be(1, $"ronde {round}");
        }
    }

    [Test]
    public async Task Deactivate_TwoAdminsDeactivateEachOtherConcurrently_OneAdminRemains()
    {
        for (int round = 0; round < Rounds; round++)
        {
            (Guid orgId, Guid a, Guid b) = await SeedOrgWithTwoAdminsAsync();
            await using ApplicationDbContext db1 = new(_options, new NoTenantContext());
            await using ApplicationDbContext db2 = new(_options, new NoTenantContext());
            TrainerService s1 = CreateService(db1);
            TrainerService s2 = CreateService(db2);

            Result[] results = await RunConcurrentlyAsync(
                () => s1.DeactivateAsync(a, orgId, b),
                () => s2.DeactivateAsync(b, orgId, a));

            results.Count(r => r.IsSuccess).Should().Be(1, $"ronde {round}: precies één deactivatie mag slagen");
            (await CountActiveAdminsAsync(orgId)).Should().Be(1, $"ronde {round}");
        }
    }

    [Test]
    public async Task Remove_TwoAdminsRemoveEachOtherConcurrently_OneAdminRemains()
    {
        for (int round = 0; round < Rounds; round++)
        {
            (Guid orgId, Guid a, Guid b) = await SeedOrgWithTwoAdminsAsync();
            await using ApplicationDbContext db1 = new(_options, new NoTenantContext());
            await using ApplicationDbContext db2 = new(_options, new NoTenantContext());
            TrainerService s1 = CreateService(db1);
            TrainerService s2 = CreateService(db2);

            Result[] results = await RunConcurrentlyAsync(
                () => s1.RemoveAsync(a, orgId, b),
                () => s2.RemoveAsync(b, orgId, a));

            results.Count(r => r.IsSuccess).Should().Be(1, $"ronde {round}: precies één verwijdering mag slagen");
            (await CountActiveAdminsAsync(orgId)).Should().Be(1, $"ronde {round}");
        }
    }
}
