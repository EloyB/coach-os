using CoachOS.Application.Abstractions;
using CoachOS.Application.Configuration;
using CoachOS.Application.WebsiteContact;
using CoachOS.Application.WebsiteContact.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using CoachOS.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Services;

[TestFixture]
public class WebsiteContactServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 5, 12, 0, 0, TimeSpan.Zero);
    private Mock<IWebsiteContactRepository> _repo = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private Mock<IEmailService> _email = null!;
    private Mock<ILogger<WebsiteContactService>> _logger = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IWebsiteContactRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _email = new Mock<IEmailService>();
        _logger = new Mock<ILogger<WebsiteContactService>>();
    }

    private WebsiteContactService Service(string recipient = "info@coach-os.be") => new(
        _repo.Object, _unitOfWork.Object, _email.Object,
        Options.Create(new WebsiteOptions { ContactNotificationEmail = recipient }),
        new AdjustableTimeProvider(Now), _logger.Object);

    private static SubmitWebsiteContactRequest Request() =>
        new("  Anna Peeters ", " Anna@Example.TEST ", "Graag meer info over CoachOS.", "  ");

    [Test]
    public async Task SubmitAsync_ValidRequest_StoresNormalizedEntryAndNotifies()
    {
        Result result = await Service().SubmitAsync(Request());

        result.IsSuccess.Should().BeTrue();
        _repo.Verify(r => r.AddAsync(It.Is<WebsiteContactEntry>(e =>
            e.Name == "Anna Peeters" && e.Email == "anna@example.test" && e.Organization == null &&
            e.CreatedAt == Now.UtcDateTime), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _email.Verify(e => e.SendWebsiteContactNotificationAsync("info@coach-os.be",
            It.Is<WebsiteContactNotification>(n => n.Email == "anna@example.test" && n.Message == "Graag meer info over CoachOS."),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task SubmitAsync_NotificationFails_StillSucceedsAndLogsWithoutPii()
    {
        _email.Setup(e => e.SendWebsiteContactNotificationAsync(It.IsAny<string>(), It.IsAny<WebsiteContactNotification>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new System.Net.Mail.SmtpException("anna@example.test geweigerd"));

        Result result = await Service().SubmitAsync(Request());

        result.IsSuccess.Should().BeTrue();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _logger.Invocations.Should().ContainSingle(i => i.Method.Name == "Log" && (LogLevel)i.Arguments[0] == LogLevel.Error);
        _logger.Invocations.Should().NotContain(i => i.Arguments.Any(a => a != null && a.ToString()!.Contains("anna@example.test")));
    }

    [Test]
    public async Task SubmitAsync_NoRecipientConfigured_StoresWithoutNotification()
    {
        Result result = await Service(recipient: "").SubmitAsync(Request());

        result.IsSuccess.Should().BeTrue();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _email.VerifyNoOtherCalls();
    }

    [Test]
    public async Task SubmitAsync_SaveFails_DoesNotNotify()
    {
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("db down"));

        Func<Task> act = () => Service().SubmitAsync(Request());

        await act.Should().ThrowAsync<InvalidOperationException>();
        _email.VerifyNoOtherCalls();
    }
}
