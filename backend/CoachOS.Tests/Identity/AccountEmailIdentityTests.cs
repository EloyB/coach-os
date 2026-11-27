using AwesomeAssertions;
using CoachOS.Application.AccountEmail;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using CoachOS.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using NSubstitute;
using NUnit.Framework;

namespace CoachOS.Tests.Identity;

[TestFixture]
public class AccountEmailIdentityTests
{
    private UserManager<ApplicationUser> _users = null!;
    private IUnitOfWork _uow = null!;
    private AccountEmailIdentity _identity = null!;
    private ApplicationUser _user = null!;

    [SetUp]
    public void SetUp()
    {
        _users = Substitute.For<UserManager<ApplicationUser>>(
            Substitute.For<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);
        _uow = Substitute.For<IUnitOfWork>();
        Microsoft.Extensions.Localization.IStringLocalizer<AccountEmailResources> text = Substitute.For<Microsoft.Extensions.Localization.IStringLocalizer<AccountEmailResources>>();
        text[Arg.Any<string>()].Returns(call => new Microsoft.Extensions.Localization.LocalizedString(call.Arg<string>(), call.Arg<string>()));
        _identity = new(_users, _uow, text);
        _user = new() { Id = Guid.NewGuid(), Email = "old@example.be", UserName = "old@example.be", IsActive = true, FirstName = "Noor" };
        _users.FindByIdAsync(_user.Id.ToString()).Returns(_user);
        _users.CheckPasswordAsync(_user, "Current123").Returns(true);
        _users.GenerateChangeEmailTokenAsync(_user, "new@example.be").Returns("bound-token");
        _users.UpdateSecurityStampAsync(_user).Returns(IdentityResult.Success);
    }

    [Test]
    public async Task Begin_ValidPassword_IssuesChallengeWithoutChangingAccount()
    {
        Result<EmailChangeChallenge> result = await _identity.BeginAsync(_user.Id, "new@example.be", "Current123");
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(new EmailChangeChallenge("Noor", "bound-token"));
        _user.Email.Should().Be("old@example.be");
        Received.InOrder(() =>
        {
            _users.UpdateSecurityStampAsync(_user);
            _users.GenerateChangeEmailTokenAsync(_user, "new@example.be");
        });
    }

    [TestCase("OLD@example.be", false)]
    [TestCase("taken@example.be", true)]
    public async Task Begin_UnavailableAddress_NeverIssuesChallenge(string address, bool taken)
    {
        if (taken) _users.FindByEmailAsync(address).Returns(new ApplicationUser());
        Result<EmailChangeChallenge> result = await _identity.BeginAsync(_user.Id, address, "Current123");
        result.IsSuccess.Should().BeFalse();
        await _users.DidNotReceiveWithAnyArgs().GenerateChangeEmailTokenAsync(default!, default!);
    }

    [Test]
    public async Task Confirm_ValidToken_ChangesEmailAndUsernameInOneTransaction()
    {
        _users.ChangeEmailAsync(_user, "new@example.be", "bound-token").Returns(IdentityResult.Success);
        _users.SetUserNameAsync(_user, "new@example.be").Returns(IdentityResult.Success);
        Result result = await _identity.ConfirmAsync(_user.Id, "new@example.be", "bound-token");
        result.IsSuccess.Should().BeTrue();
        Received.InOrder(() =>
        {
            _uow.BeginTransactionAsync(Arg.Any<CancellationToken>());
            _users.ChangeEmailAsync(_user, "new@example.be", "bound-token");
            _users.SetUserNameAsync(_user, "new@example.be");
            _uow.CommitTransactionAsync(Arg.Any<CancellationToken>());
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Confirm_IdentityFailure_RollsBackWithoutCommitting(bool renameFailure)
    {
        IdentityResult failure = IdentityResult.Failed(new IdentityError { Code = "InvalidToken" });
        _users.ChangeEmailAsync(_user, "new@example.be", "bad-token").Returns(renameFailure ? IdentityResult.Success : failure);
        _users.SetUserNameAsync(_user, "new@example.be").Returns(failure);
        Result result = await _identity.ConfirmAsync(_user.Id, "new@example.be", "bad-token");
        result.IsSuccess.Should().BeFalse();
        await _uow.Received(1).RollbackTransactionAsync(Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().CommitTransactionAsync(Arg.Any<CancellationToken>());
        if (!renameFailure) await _users.DidNotReceiveWithAnyArgs().SetUserNameAsync(default!, default!);
    }

    [Test]
    public async Task Begin_WrongPassword_NeverIssuesChallenge()
    {
        Result<EmailChangeChallenge> result = await _identity.BeginAsync(_user.Id, "new@example.be", "wrong");
        result.IsSuccess.Should().BeFalse();
        await _users.DidNotReceiveWithAnyArgs().GenerateChangeEmailTokenAsync(default!, default!);
        _user.Email.Should().Be("old@example.be");
    }

    [Test]
    public async Task Confirm_UniqueConstraintRace_ReturnsValidationAndRollsBack()
    {
        _users.ChangeEmailAsync(_user, "new@example.be", "bound-token").Returns(IdentityResult.Success);
        _users.SetUserNameAsync(_user, "new@example.be").Returns<Task<IdentityResult>>(_ =>
            throw new Microsoft.EntityFrameworkCore.DbUpdateException("duplicate", new Npgsql.PostgresException("duplicate", "ERROR", "ERROR", "23505")));
        Result result = await _identity.ConfirmAsync(_user.Id, "new@example.be", "bound-token");
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Validation);
        await _uow.Received(1).RollbackTransactionAsync(Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().CommitTransactionAsync(Arg.Any<CancellationToken>());
    }
}
