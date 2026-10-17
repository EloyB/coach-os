using CoachOS.Application.Abstractions;
using CoachOS.Application.Auth.DTOs;
using CoachOS.Application.Configuration;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Models;
using CoachOS.Infrastructure.Identity;
using CoachOS.Infrastructure.Persistence;
using CoachOS.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Identity;

/// <summary>
/// Toetst de globale noodrem op zelfregistratie in <see cref="AuthService"/>: boven een maximum
/// aantal nieuwe organisaties per uur weigert registratie, zodat een bot die over veel IP's
/// registreert ons niet ongelimiteerd verificatiemails laat versturen.
/// </summary>
[TestFixture]
public class AuthServiceRegistrationBrakeTests
{
    private const int MaxPerHour = 3;

    private ApplicationDbContext _db = null!;
    private Mock<UserManager<ApplicationUser>> _userManager = null!;
    private Mock<IEmailService> _email = null!;

    private sealed class NoTenantContext : ITenantContext
    {
        public Guid OrganizationId => Guid.Empty;
        public Guid UserId => Guid.Empty;
        public bool IsAuthenticated => false;
    }

    [SetUp]
    public void SetUp()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _db = new ApplicationDbContext(options, new NoTenantContext());

        _userManager = new Mock<UserManager<ApplicationUser>>(
            Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);
        _userManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);
        _userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        _email = new Mock<IEmailService>();
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private AuthService CreateSut(TimeProvider timeProvider)
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-signing-key-at-least-32-bytes-long!!",
                ["Jwt:Issuer"] = "coachos-test",
                ["Jwt:Audience"] = "coachos-test",
                ["Jwt:ExpiryMinutes"] = "60",
            })
            .Build();

        RegistrationOptions registrationOptions = new() { MaxRegistrationsPerHour = MaxPerHour };

        return new AuthService(
            _userManager.Object,
            _db,
            new TokenService(config),
            _email.Object,
            Options.Create(registrationOptions),
            timeProvider,
            NullLogger<AuthService>.Instance);
    }

    private async Task SeedOrganizationsAsync(int count)
    {
        for (int i = 0; i < count; i++)
            _db.Organizations.Add(new Organization { Id = Guid.NewGuid(), Name = $"Club {i}", Email = $"club{i}@test.be" });
        await _db.SaveChangesAsync();
    }

    private static RegisterRequest NewRequest() => new()
    {
        OrganizationName = "Tennis Nieuw",
        FirstName = "Nora",
        LastName = "Nieuw",
        Email = "nora@nieuw.be",
        Password = "Sterk1234!",
        AcceptTerms = true,
        AcceptDataProcessingAgreement = true,
    };

    [Test]
    public async Task RegisterAsync_LimitReachedWithinLastHour_ReturnsTooManyRequestsWithoutSideEffects()
    {
        await SeedOrganizationsAsync(MaxPerHour);
        AuthService sut = CreateSut(TimeProvider.System);

        Result<AuthResponseDto> result = await sut.RegisterAsync(NewRequest());

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.TooManyRequests);
        (await _db.Organizations.CountAsync()).Should().Be(MaxPerHour);
        _userManager.Verify(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        _email.VerifyNoOtherCalls();
    }

    [Test]
    public async Task RegisterAsync_BelowLimit_CreatesOrganization()
    {
        await SeedOrganizationsAsync(MaxPerHour - 1);
        AuthService sut = CreateSut(TimeProvider.System);

        Result<AuthResponseDto> result = await sut.RegisterAsync(NewRequest());

        result.IsSuccess.Should().BeTrue();
        (await _db.Organizations.CountAsync()).Should().Be(MaxPerHour);
    }

    [Test]
    public async Task RegisterAsync_RegistrationsOlderThanOneHour_DoNotCount()
    {
        await SeedOrganizationsAsync(MaxPerHour);
        // Klok twee uur vooruit: de bestaande organisaties vallen buiten het venster van één uur.
        AuthService sut = CreateSut(new FixedTimeProvider(DateTimeOffset.UtcNow.AddHours(2)));

        Result<AuthResponseDto> result = await sut.RegisterAsync(NewRequest());

        result.IsSuccess.Should().BeTrue();
    }
}
