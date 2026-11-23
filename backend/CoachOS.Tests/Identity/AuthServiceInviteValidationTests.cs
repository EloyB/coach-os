using AwesomeAssertions;
using CoachOS.API.Extensions;
using CoachOS.Application.Abstractions;
using CoachOS.Application.Auth.DTOs;
using CoachOS.Application.Configuration;
using CoachOS.Domain.Models;
using CoachOS.Infrastructure.Identity;
using CoachOS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Identity;

[TestFixture]
public class AuthServiceInviteValidationTests
{
    private const string InviteToken = "test-invite-token";
    private ApplicationDbContext _db = null!;
    private AuthService _service = null!;

    [SetUp]
    public void SetUp()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options, Mock.Of<ITenantContext>());
        Mock<UserManager<ApplicationUser>> userManager = new(
            Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);
        userManager.Setup(manager => manager.Users).Returns(_db.Users);
        _service = new AuthService(
            userManager.Object, _db, new TokenService(new ConfigurationBuilder().Build()),
            Mock.Of<IEmailService>(), Options.Create(new RegistrationOptions()),
            TimeProvider.System, NullLogger<AuthService>.Instance);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [TestCase("", "Ongeldige uitnodigingslink")]
    [TestCase("   ", "Ongeldige uitnodigingslink")]
    [TestCase("unknown-token", "Ongeldige uitnodigingslink")]
    [TestCase("expired-token", "Uitnodigingslink is verlopen")]
    [TestCase("missing-expiry-token", "Uitnodigingslink is verlopen")]
    public async Task ValidateInviteTokenAsync_InvalidInvite_ReturnsValidationErrorMappedToBadRequest(
        string token, string expectedMessage)
    {
        await SeedInviteAsync("expired-token", DateTime.UtcNow.AddHours(-1));
        await SeedInviteAsync("missing-expiry-token", null);

        Result<InviteValidationDto> result = await _service.ValidateInviteTokenAsync(token, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        IStatusCodeHttpResult response = (IStatusCodeHttpResult)result.ToErrorResult();
        response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(ErrorCodes.Validation);
        result.Errors[0].Message.Should().Be(expectedMessage);
        IValueHttpResult body = (IValueHttpResult)response;
        ((IEnumerable<string>)body.Value!).Should().Equal(expectedMessage);
    }

    [Test]
    public async Task ValidateInviteTokenAsync_ValidInvite_ReturnsFirstNameWithoutConsumingInvite()
    {
        await SeedInviteAsync(InviteToken, DateTime.UtcNow.AddHours(1));

        Result<InviteValidationDto> result = await _service.ValidateInviteTokenAsync(InviteToken, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(new InviteValidationDto("Noor"));
        result.Errors.Should().BeEmpty();
        _db.ChangeTracker.Clear();
        ApplicationUser user = await _db.Users.SingleAsync();
        user.InviteToken.Should().NotBeNullOrEmpty();
        user.InviteTokenExpiry.Should().NotBeNull();
    }

    private async Task SeedInviteAsync(string token, DateTime? expiry)
    {
        byte[] tokenBytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
        _db.Users.Add(new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FirstName = "Noor",
            InviteToken = Convert.ToHexStringLower(tokenBytes),
            InviteTokenExpiry = expiry
        });
        await _db.SaveChangesAsync(CancellationToken.None);
    }
}
