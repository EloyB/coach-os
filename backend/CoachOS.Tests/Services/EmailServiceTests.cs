using CoachOS.Infrastructure.Email;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Services;

[TestFixture]
public class EmailServiceTests
{
    private Mock<IMjmlTemplateRenderer> _renderer = null!;
    private EmailService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _renderer = new Mock<IMjmlTemplateRenderer>();

        // SMTP-config wijst naar onbestaande host — SendAsync zal SmtpException
        // gooien NA dat renderer.Render is aangeroepen, dus we vangen die in de
        // tests op. Doel van deze tests is enkel de tokens-dictionary te
        // verifiëren (dayName-conversie), niet de SMTP-call.
        EmailOptions options = new()
        {
            SmtpHost = "localhost",
            SmtpPort = 1,
            FromAddress = "test@coach-os.be",
            FromName = "CoachOS Test",
            EnableSsl = false,
            Username = "u",
            Password = "p",
        };

        _sut = new EmailService(
            Options.Create(options),
            _renderer.Object,
            NullLogger<EmailService>.Instance);
    }

    [Test]
    public async Task SendEnrollmentConfirmation_OmitsTrainerDetailsWhenNoTrainerIsAssigned()
    {
        IReadOnlyDictionary<string, string>? captured = null;
        _renderer
            .Setup(r => r.Render("enrollment-confirmation", It.IsAny<IReadOnlyDictionary<string, string>>()))
            .Callback<string, IReadOnlyDictionary<string, string>>((_, dict) => captured = dict)
            .Returns("<html/>");

        try
        {
            await _sut.SendEnrollmentConfirmationAsync(
                "a@b.be", "Anna", "Tennisreeks 1", string.Empty, null, CancellationToken.None);
        }
        catch
        {
            // SMTP-failure verwacht; renderer.Render is reeds aangeroepen.
        }

        captured.Should().NotBeNull();
        captured!["trainerDescription"].Should().Be("Je club neemt indien nodig contact met je op.");
        captured["trainerLine"].Should().BeEmpty();
    }

    [TestCase(0, "maandag")]   // EU 0 = maandag
    [TestCase(1, "dinsdag")]   // EU 1 = dinsdag (de bug-case: gaf voorheen "maandag")
    [TestCase(2, "woensdag")]
    [TestCase(3, "donderdag")]
    [TestCase(4, "vrijdag")]
    [TestCase(5, "zaterdag")]
    [TestCase(6, "zondag")]    // EU 6 = zondag
    public async Task SendScheduleConfirmation_RendersCorrectDutchDayName(int euDay, string expectedDayName)
    {
        // Arrange
        IReadOnlyDictionary<string, string>? captured = null;
        _renderer
            .Setup(r => r.Render("schedule-confirmation", It.IsAny<IReadOnlyDictionary<string, string>>()))
            .Callback<string, IReadOnlyDictionary<string, string>>((_, dict) => captured = dict)
            .Returns("<html/>");

        // Act — SMTP-send zal falen, dat is OK; we testen alleen renderer-input
        try
        {
            await _sut.SendScheduleConfirmationAsync(
                "a@b.be", "Anna", "Tennisreeks 1", euDay,
                "18:00", "19:00", null, "https://x", null, CancellationToken.None);
        }
        catch
        {
            // SMTP-failure verwacht; renderer.Render is reeds aangeroepen
        }

        // Assert
        captured.Should().NotBeNull();
        captured!["dayName"].Should().Be(expectedDayName);
    }
}
