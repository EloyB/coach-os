using AwesomeAssertions;
using CoachOS.Application.AccountEmail;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using CoachOS.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Identity;

[TestFixture]
public class AccountEmailIdentityTests
{
    private Mock<UserManager<ApplicationUser>> _users = null!;
    private Mock<IUnitOfWork> _uow = null!;
    private AccountEmailIdentity _identity = null!;
    private ApplicationUser _user = null!;
    private List<string> _calls = null!;

    [SetUp]
    public void SetUp()
    {
        _calls = [];
        _users = new Mock<UserManager<ApplicationUser>>(
            Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);
        _uow = new Mock<IUnitOfWork>();
        _uow.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Callback(() => _calls.Add("begin")).Returns(Task.CompletedTask);
        _uow.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>())).Callback(() => _calls.Add("commit")).Returns(Task.CompletedTask);
        var text = new Mock<IStringLocalizer<AccountEmailResources>>();
        text.Setup(t => t[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        _identity = new(_users.Object, _uow.Object, text.Object);
        _user = new() { Id = Guid.NewGuid(), Email = "old@example.be", UserName = "old@example.be", IsActive = true, FirstName = "Noor" };
        _users.Setup(u => u.FindByIdAsync(_user.Id.ToString())).ReturnsAsync(_user);
        _users.Setup(u => u.CheckPasswordAsync(_user, "Current123")).ReturnsAsync(true);
        _users.Setup(u => u.GenerateChangeEmailTokenAsync(_user, "new@example.be")).Callback(() => _calls.Add("token")).ReturnsAsync("bound-token");
        _users.Setup(u => u.UpdateSecurityStampAsync(_user)).Callback(() => _calls.Add("stamp")).ReturnsAsync(IdentityResult.Success);
    }

    [Test]
    public async Task Begin_ValidPassword_IssuesChallengeWithoutChangingAccount()
    {
        var result = await _identity.BeginAsync(_user.Id, "new@example.be", "Current123");
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(new EmailChangeChallenge("Noor", "bound-token"));
        _user.Email.Should().Be("old@example.be");
        _calls.Should().Equal("stamp", "token");
    }

    [TestCase("OLD@example.be", false)]
    [TestCase("taken@example.be", true)]
    public async Task Begin_UnavailableAddress_NeverIssuesChallenge(string address, bool taken)
    {
        if (taken) _users.Setup(u => u.FindByEmailAsync(address)).ReturnsAsync(new ApplicationUser());
        var result = await _identity.BeginAsync(_user.Id, address, "Current123");
        result.IsSuccess.Should().BeFalse();
        _users.Verify(u => u.GenerateChangeEmailTokenAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task Confirm_ValidToken_ChangesEmailAndUsernameInOneTransaction()
    {
        _users.Setup(u => u.ChangeEmailAsync(_user, "new@example.be", "bound-token")).Callback(() => _calls.Add("email")).ReturnsAsync(IdentityResult.Success);
        _users.Setup(u => u.SetUserNameAsync(_user, "new@example.be")).Callback(() => _calls.Add("username")).ReturnsAsync(IdentityResult.Success);
        var result = await _identity.ConfirmAsync(_user.Id, "new@example.be", "bound-token");
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(new EmailChanged("old@example.be", "Noor"));
        _calls.Should().Equal("begin", "email", "username", "commit");
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Confirm_IdentityFailure_RollsBackWithoutCommitting(bool renameFailure)
    {
        var failure = IdentityResult.Failed(new IdentityError { Code = "InvalidToken" });
        _users.Setup(u => u.ChangeEmailAsync(_user, "new@example.be", "bad-token")).ReturnsAsync(renameFailure ? IdentityResult.Success : failure);
        _users.Setup(u => u.SetUserNameAsync(_user, "new@example.be")).ReturnsAsync(failure);
        var result = await _identity.ConfirmAsync(_user.Id, "new@example.be", "bad-token");
        result.IsSuccess.Should().BeFalse();
        _uow.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        if (!renameFailure) _users.Verify(u => u.SetUserNameAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task Begin_WrongPassword_NeverIssuesChallenge()
    {
        var result = await _identity.BeginAsync(_user.Id, "new@example.be", "wrong");
        result.IsSuccess.Should().BeFalse();
        _users.Verify(u => u.GenerateChangeEmailTokenAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        _user.Email.Should().Be("old@example.be");
    }

    [Test]
    public async Task Confirm_UniqueConstraintRace_ReturnsValidationAndRollsBack()
    {
        _users.Setup(u => u.ChangeEmailAsync(_user, "new@example.be", "bound-token")).ReturnsAsync(IdentityResult.Success);
        _users.Setup(u => u.SetUserNameAsync(_user, "new@example.be")).ThrowsAsync(
            new Microsoft.EntityFrameworkCore.DbUpdateException("duplicate", new Npgsql.PostgresException("duplicate", "ERROR", "ERROR", "23505")));
        var result = await _identity.ConfirmAsync(_user.Id, "new@example.be", "bound-token");
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.Validation);
        _uow.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
