using System.Data;
using AwesomeAssertions;
using CoachOS.Application.Common;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Common;

[TestFixture]
public class UnitOfWorkExtensionsTests
{
    private Mock<IUnitOfWork> _unitOfWork = null!;

    [SetUp]
    public void SetUp() => _unitOfWork = new Mock<IUnitOfWork>();

    [Test]
    public async Task Commits_when_the_work_succeeds()
    {
        var result = await _unitOfWork.Object.ExecuteInTransactionAsync(
            IsolationLevel.Serializable, () => Task.FromResult(Result<int>.Ok(7)));

        result.Value.Should().Be(7);
        _unitOfWork.Verify(u => u.BeginTransactionAsync(IsolationLevel.Serializable, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Rolls_back_when_the_work_returns_a_failure()
    {
        var result = await _unitOfWork.Object.ExecuteInTransactionAsync(
            IsolationLevel.Serializable, () => Task.FromResult(Result<int>.Fail(new Error(ErrorCodes.Conflict, "vol"))));

        result.IsSuccess.Should().BeFalse();
        _unitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Rolls_back_and_rethrows_when_the_commit_throws()
    {
        _unitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("commit kapot"));

        Func<Task> act = () => _unitOfWork.Object.ExecuteInTransactionAsync(
            IsolationLevel.Serializable, () => Task.FromResult(Result<int>.Ok(1)));

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("commit kapot");
        _unitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
