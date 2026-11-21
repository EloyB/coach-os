using AwesomeAssertions;
using CoachOS.Application.Abstractions;
using CoachOS.Application.Configuration;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using CoachOS.Infrastructure.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Identity;

[TestFixture]
public class StudentMagicLinkServiceTests
{
    // Onbekend, verlopen en al gebruikt: TryConsumeAsync geeft in alle gevallen null.
    // De TokenService wordt op dit pad niet aangeroepen.
    private static StudentMagicLinkService CreateService() => new(
        Mock.Of<IMagicLinkTokenRepository>(r =>
            r.TryConsumeAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())
            == Task.FromResult<MagicLinkToken?>(null)),
        Mock.Of<IUnitOfWork>(),
        Mock.Of<IEmailService>(),
        null!,
        Options.Create(new AppOptions()),
        NullLogger<StudentMagicLinkService>.Instance);

    [Test]
    public async Task Redeem_UsedOrExpiredToken_ReturnsGoneInsteadOfUnexpected()
    {
        var result = await CreateService().RedeemAsync("al-gebruikt-of-verlopen");

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be(ErrorCodes.Gone);
    }

    [Test]
    public async Task Redeem_UnknownToken_ReturnsSameGoneError()
    {
        var result = await CreateService().RedeemAsync("bestaat-niet");

        result.Errors[0].Code.Should().Be(ErrorCodes.Gone);
    }
}
