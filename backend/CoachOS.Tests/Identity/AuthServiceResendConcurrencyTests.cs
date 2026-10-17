using System.Collections.Concurrent;
using CoachOS.Application.Abstractions;
using CoachOS.Application.Auth;
using CoachOS.Application.Configuration;
using CoachOS.Domain.Models;
using CoachOS.Infrastructure.Identity;
using CoachOS.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Npgsql;
using NUnit.Framework;

namespace CoachOS.Tests.Identity;

/// <summary>
/// Bewijst dat "nieuwe verificatiecode aanvragen" ook onder gelijktijdige requests hooguit één
/// mail verstuurt: de wachttijd (1 per minuut) en het maximum aantal codes moeten atomair in de
/// database geclaimd worden. Vereist een echte Postgres (ExecuteUpdate + gelijktijdige
/// connecties); opt-in via COACHOS_TEST_PG, zie <see cref="TrainerServiceConcurrencyTests"/>.
/// </summary>
[TestFixture]
[NonParallelizable]
public class AuthServiceResendConcurrencyTests
{
    private const int Rounds = 10;
    private const int ConcurrentRequests = 8;

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

    private static AuthService CreateService(ApplicationDbContext db, IEmailService email)
    {
        Mock<UserManager<ApplicationUser>> userManager = new(
            Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);
        userManager.Setup(m => m.Users).Returns(db.Users);
        // Zelfde gedrag als de EF UserStore: tracked ophalen, en bij opslaan de ConcurrencyStamp
        // vernieuwen en een concurrency-conflict als IdentityResult teruggeven.
        userManager.Setup(m => m.FindByIdAsync(It.IsAny<string>()))
            .Returns<string>(async id => await db.Users.FindAsync(Guid.Parse(id)));
        userManager.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>()))
            .Returns<ApplicationUser>(async user =>
            {
                user.ConcurrencyStamp = Guid.NewGuid().ToString();
                try
                {
                    await db.SaveChangesAsync();
                    return IdentityResult.Success;
                }
                catch (DbUpdateConcurrencyException)
                {
                    return IdentityResult.Failed(new IdentityErrorDescriber().ConcurrencyFailure());
                }
            });

        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-signing-key-at-least-32-bytes-long!!",
                ["Jwt:Issuer"] = "coachos-test",
                ["Jwt:Audience"] = "coachos-test",
                ["Jwt:ExpiryMinutes"] = "60",
            })
            .Build();

        return new AuthService(
            userManager.Object, db, new TokenService(config), email,
            Options.Create(new RegistrationOptions()), TimeProvider.System, NullLogger<AuthService>.Instance);
    }

    private async Task<Guid> SeedUnverifiedUserAsync(int sendCount)
    {
        await using ApplicationDbContext db = new(_options, new NoTenantContext());
        ApplicationUser user = new()
        {
            Id = Guid.NewGuid(),
            UserName = $"{Guid.NewGuid():N}@test.be",
            Email = $"{Guid.NewGuid():N}@test.be",
            FirstName = "Vera",
            LastName = "Verify",
            IsActive = true,
            EmailConfirmed = false,
            // Vorige code is ruim buiten de wachttijd verstuurd: precies één nieuwe mag nu.
            EmailVerificationSentAt = DateTime.UtcNow.AddMinutes(-5),
            EmailVerificationSendCount = sendCount,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }

    private async Task<ApplicationUser> LoadUserAsync(Guid userId)
    {
        await using ApplicationDbContext db = new(_options, new NoTenantContext());
        return await db.Users.AsNoTracking().SingleAsync(u => u.Id == userId);
    }

    /// <summary>Start alle resends tegelijk, elk met een eigen DbContext (= eigen request).</summary>
    private async Task<(Result<bool>[] Results, ConcurrentBag<string> MailedCodes)> ResendConcurrentlyAsync(Guid userId)
    {
        ConcurrentBag<string> mailedCodes = [];
        Mock<IEmailService> email = new();
        email.Setup(e => e.SendEmailVerificationAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((_, _, code, _) => mailedCodes.Add(code))
            .Returns(Task.CompletedTask);

        List<ApplicationDbContext> contexts = [];
        try
        {
            TaskCompletionSource start = new(TaskCreationOptions.RunContinuationsAsynchronously);
            Task<Result<bool>>[] tasks = Enumerable.Range(0, ConcurrentRequests)
                .Select(_ =>
                {
                    ApplicationDbContext db = new(_options, new NoTenantContext());
                    contexts.Add(db);
                    AuthService sut = CreateService(db, email.Object);
                    return Task.Run(async () =>
                    {
                        await start.Task;
                        return await sut.ResendEmailVerificationAsync(userId);
                    });
                })
                .ToArray();
            start.SetResult();
            return (await Task.WhenAll(tasks), mailedCodes);
        }
        finally
        {
            foreach (ApplicationDbContext db in contexts)
                await db.DisposeAsync();
        }
    }

    /// <summary>
    /// Wie de race verliest, krijgt dezelfde nette melding als bij een gewone dubbele klik, en
    /// geen technische fout (zoals Identity's "Optimistic concurrency failure").
    /// </summary>
    private static void AssertLosersGetCooldownMessage(Result<bool>[] results, int round)
    {
        results.Where(r => !r.IsSuccess)
            .Should().OnlyContain(
                r => r.Errors[0].Code == ErrorCodes.Validation
                     && r.Errors[0].Message == "Wacht even voor je een nieuwe code aanvraagt.",
                $"ronde {round}");
    }

    [Test]
    public async Task Resend_ConcurrentRequests_SendExactlyOneMailWithTheStoredCode()
    {
        for (int round = 0; round < Rounds; round++)
        {
            Guid userId = await SeedUnverifiedUserAsync(sendCount: 1);

            (Result<bool>[] results, ConcurrentBag<string> mailedCodes) = await ResendConcurrentlyAsync(userId);

            results.Count(r => r.IsSuccess && r.Value).Should().Be(1, $"ronde {round}: precies één resend mag slagen");
            AssertLosersGetCooldownMessage(results, round);
            mailedCodes.Should().HaveCount(1, $"ronde {round}: precies één mail");

            ApplicationUser stored = await LoadUserAsync(userId);
            stored.EmailVerificationSendCount.Should().Be(2, $"ronde {round}");
            stored.EmailVerificationCodeHash.Should().Be(
                EmailVerificationCodes.Hash(userId, mailedCodes.Single()),
                $"ronde {round}: de gemailde code moet de geldige code zijn");
        }
    }

    [Test]
    public async Task Resend_ConcurrentRequestsAtLastAllowedSend_NeverExceedMaxSends()
    {
        for (int round = 0; round < Rounds; round++)
        {
            Guid userId = await SeedUnverifiedUserAsync(sendCount: EmailVerificationCodes.MaxSends - 1);

            (Result<bool>[] results, ConcurrentBag<string> mailedCodes) = await ResendConcurrentlyAsync(userId);

            mailedCodes.Should().HaveCount(1, $"ronde {round}");
            results.Count(r => r.IsSuccess && r.Value).Should().Be(1, $"ronde {round}");
            AssertLosersGetCooldownMessage(results, round);
            (await LoadUserAsync(userId)).EmailVerificationSendCount
                .Should().Be(EmailVerificationCodes.MaxSends, $"ronde {round}");
        }
    }
}
