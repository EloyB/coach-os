using AwesomeAssertions;
using CoachOS.Application.Abstractions;
using CoachOS.Application.AccountEmail;
using CoachOS.Application.Auth;
using CoachOS.Application.Auth.DTOs;
using CoachOS.Application.Configuration;
using CoachOS.Domain.Models;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Identity;

[TestFixture]
public class AccountEmailServiceTests
{
    [TestCase(true)]
    [TestCase(false)]
    public async Task Confirm_RefreshesSessionAndNotifiesOldAddressOnlyAfterIdentitySuccess(bool valid)
    {
        var identity = new Mock<IAccountEmailIdentity>();
        var auth = new Mock<IAuthService>();
        var email = new Mock<IEmailService>();
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        identity.Setup(i => i.ConfirmAsync(userId, "new@example.be", "token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(valid
                ? Result<EmailChanged>.Ok(new("old@example.be", "Noor"))
                : Result<EmailChanged>.Fail(new Error(ErrorCodes.Validation, "Ongeldig")));
        auth.Setup(a => a.SwitchOrganizationAsync(userId, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AuthResponseDto>.Ok(new() { Email = "new@example.be", EmailVerified = true }));
        var service = new AccountEmailService(identity.Object, email.Object, auth.Object, Options.Create(new AppOptions()));

        var result = await service.ConfirmAsync(userId, orgId, new(" new@example.be ", "token"));

        result.IsSuccess.Should().Be(valid);
        var times = valid ? Times.Once() : Times.Never();
        auth.Verify(a => a.SwitchOrganizationAsync(userId, orgId, It.IsAny<CancellationToken>()), times);
        email.Verify(e => e.SendAccountEmailChangedNoticeAsync("old@example.be", "Noor", "new@example.be", It.IsAny<CancellationToken>()), times);
    }

    [Test]
    public async Task Request_SendsBoundFragmentLinkOnlyAfterPasswordValidation()
    {
        var identity = new Mock<IAccountEmailIdentity>();
        var email = new Mock<IEmailService>();
        var userId = Guid.NewGuid();
        identity.Setup(i => i.BeginAsync(userId, "new@example.be", "Current123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<EmailChangeChallenge>.Ok(new("Noor", "secret+token")));
        var service = new AccountEmailService(identity.Object, email.Object, Mock.Of<IAuthService>(),
            Options.Create(new AppOptions { FrontendBaseUrl = "https://app.example.be" }));

        var result = await service.RequestAsync(userId, new(" new@example.be ", "Current123"));

        result.IsSuccess.Should().BeTrue();
        email.Verify(e => e.SendAccountEmailChangeAsync("new@example.be", "Noor",
            "https://app.example.be/dashboard/account#email=new%40example.be&token=secret%2Btoken",
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
