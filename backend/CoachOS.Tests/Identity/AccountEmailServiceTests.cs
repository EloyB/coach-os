using AwesomeAssertions;
using CoachOS.Application.Abstractions;
using CoachOS.Application.AccountEmail;
using CoachOS.Application.AccountEmail.DTOs;
using CoachOS.Application.Auth;
using CoachOS.Application.Configuration;
using CoachOS.Domain.Models;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace CoachOS.Tests.Identity;

[TestFixture]
public class AccountEmailServiceTests
{
    [TestCase(true)]
    [TestCase(false)]
    public async Task Confirm_RefreshesCurrentOrganizationOnlyAfterIdentitySuccess(bool valid)
    {
        IAccountEmailIdentity identity = Substitute.For<IAccountEmailIdentity>();
        IAuthService auth = Substitute.For<IAuthService>();
        Guid userId = Guid.NewGuid();
        Guid orgId = Guid.NewGuid();
        identity.ConfirmAsync(userId, "new@example.be", "token", Arg.Any<CancellationToken>())
            .Returns(valid ? Result.Ok() : Result.Fail(new Error(ErrorCodes.Validation, "Ongeldig")));
        auth.SwitchOrganizationAsync(userId, orgId, Arg.Any<CancellationToken>())
            .Returns(Result<CoachOS.Application.Auth.DTOs.AuthResponseDto>.Ok(new() { Email = "new@example.be", EmailVerified = true }));
        AccountEmailService service = new(identity, Substitute.For<IEmailService>(), auth, Options.Create(new AppOptions()));
        Result<CoachOS.Application.Auth.DTOs.AuthResponseDto> result = await service.ConfirmAsync(userId, orgId, new("new@example.be", "token"));
        result.IsSuccess.Should().Be(valid);
        await auth.Received(valid ? 1 : 0).SwitchOrganizationAsync(userId, orgId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Request_SendsBoundFragmentLinkOnlyAfterPasswordValidation()
    {
        IAccountEmailIdentity identity = Substitute.For<IAccountEmailIdentity>();
        IEmailService email = Substitute.For<IEmailService>();
        Guid userId = Guid.NewGuid();
        identity.BeginAsync(userId, "new@example.be", "Current123", Arg.Any<CancellationToken>())
            .Returns(Result<EmailChangeChallenge>.Ok(new("Noor", "secret+token")));
        AccountEmailService service = new(identity, email, Substitute.For<IAuthService>(),
            Options.Create(new AppOptions { FrontendBaseUrl = "https://app.example.be" }));

        Result result = await service.RequestAsync(userId, new(" new@example.be ", "Current123"));

        result.IsSuccess.Should().BeTrue();
        await email.Received(1).SendAccountEmailChangeAsync("new@example.be", "Noor",
            "https://app.example.be/dashboard/account#email=new%40example.be&token=secret%2Btoken",
            Arg.Any<CancellationToken>());
    }
}
