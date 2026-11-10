using CoachOS.Application.WebsiteContact;
using CoachOS.Application.WebsiteWaitlist;
using CoachOS.Application.WebsiteWaitlist.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using CoachOS.Tests.TestHelpers;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Services;

/// <summary>Pure unit tests (geen PostgreSQL nodig); zie WebsiteWaitlistTests voor de PG-integratie.</summary>
[TestFixture]
public class WebsiteWaitlistServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 5, 12, 0, 0, TimeSpan.Zero);
    private Mock<IWebsiteWaitlistRepository> _repo = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IWebsiteWaitlistRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
    }

    private WebsiteWaitlistService Service(DateTimeOffset now) => new(_repo.Object, _unitOfWork.Object, new AdjustableTimeProvider(now));

    [Test]
    public async Task RegisterAsync_NewEmail_StoresHashOnlyEntryAndMailWithRawToken()
    {
        WebsiteWaitlistEntry? entry = null;
        WebsiteWaitlistMail? mail = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<WebsiteWaitlistEntry>(), It.IsAny<WebsiteWaitlistMail>(), It.IsAny<CancellationToken>()))
            .Callback<WebsiteWaitlistEntry, WebsiteWaitlistMail, CancellationToken>((e, m, _) => { entry = e; mail = m; });

        Result result = await Service(Now).RegisterAsync(new RegisterWebsiteWaitlistRequest(" Owner@Example.TEST ", "club"));

        result.IsSuccess.Should().BeTrue();
        entry!.Email.Should().Be("owner@example.test");
        entry.UnsubscribeTokenHash.Should().Be(WebsiteWaitlistService.Hash(mail!.Token)).And.NotBe(mail.Token);
        mail.EntryId.Should().Be(entry.Id);
        mail.ExpiresAt.Should().Be(Now.UtcDateTime.AddDays(7));
        _unitOfWork.Verify(u => u.TrySaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task RegisterAsync_ExistingEmail_ReturnsOkWithoutNewMail()
    {
        _repo.Setup(r => r.ExistsAsync("owner@example.test", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        Result result = await Service(Now).RegisterAsync(new RegisterWebsiteWaitlistRequest("owner@example.test"));

        result.IsSuccess.Should().BeTrue();
        _repo.Verify(r => r.AddAsync(It.IsAny<WebsiteWaitlistEntry>(), It.IsAny<WebsiteWaitlistMail>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task RegisterAsync_AfterDeadline_ReturnsGone()
    {
        Result result = await Service(WebsiteWaitlistService.RetentionDeadline).RegisterAsync(new RegisterWebsiteWaitlistRequest("late@example.test"));

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.Gone);
        _repo.VerifyNoOtherCalls();
    }

    [Test]
    public async Task RegisterAsync_CloseToDeadline_MailExpiresAtDeadline()
    {
        WebsiteWaitlistMail? mail = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<WebsiteWaitlistEntry>(), It.IsAny<WebsiteWaitlistMail>(), It.IsAny<CancellationToken>()))
            .Callback<WebsiteWaitlistEntry, WebsiteWaitlistMail, CancellationToken>((_, m, _) => mail = m);

        await Service(WebsiteWaitlistService.RetentionDeadline.AddDays(-1)).RegisterAsync(new RegisterWebsiteWaitlistRequest("x@example.test"));

        mail!.ExpiresAt.Should().Be(WebsiteWaitlistService.RetentionDeadline);
    }

    [Test]
    public async Task UnsubscribeAsync_RemovesByHashInsideTransaction()
    {
        string token = new('a', 64);

        Result result = await Service(Now).UnsubscribeAsync(new UnsubscribeWebsiteWaitlistRequest(token));

        result.IsSuccess.Should().BeTrue();
        _repo.Verify(r => r.RemoveByHashAsync(WebsiteWaitlistService.Hash(token), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task QueueManagedMailAsync_DryRun_ReportsEligibleWithoutQueueing()
    {
        Guid id = Guid.NewGuid();
        _repo.Setup(r => r.FindForMailAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(new WebsiteWaitlistEntry { Id = id });

        Result<ManagedWebsiteWaitlistMailResult> result = await Service(Now).QueueManagedMailAsync(id, apply: false);

        result.Value.Should().Be(new ManagedWebsiteWaitlistMailResult(true, false));
        _repo.Verify(r => r.AddMailAsync(It.IsAny<WebsiteWaitlistMail>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task QueueManagedMailAsync_PendingMail_IsNotEligible()
    {
        Guid id = Guid.NewGuid();
        _repo.Setup(r => r.FindForMailAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(new WebsiteWaitlistEntry { Id = id });
        _repo.Setup(r => r.FindMailAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(new WebsiteWaitlistMail { EntryId = id });

        Result<ManagedWebsiteWaitlistMailResult> result = await Service(Now).QueueManagedMailAsync(id, apply: true);

        result.Value.Should().Be(new ManagedWebsiteWaitlistMailResult(false, false));
    }

    [TestCase("2025-02-28T12:00:00Z", "2024-02-29T12:00:00Z")]
    [TestCase("2025-03-01T00:00:00Z", "2024-03-01T00:00:00Z")]
    [TestCase("2026-11-05T12:00:00Z", "2025-11-05T12:00:00Z")]
    [TestCase("2028-02-29T00:00:00Z", "2027-02-28T00:00:00Z")]
    public void ContactRetentionCutoff_IsTwelveCalendarMonthsWithLeapDayClamp(string now, string expectedCutoff)
    {
        DateTime utcNow = DateTimeOffset.Parse(now).UtcDateTime;

        WebsiteContactRetention.CutoffFor(utcNow).Should().Be(DateTimeOffset.Parse(expectedCutoff).UtcDateTime);
    }
}
