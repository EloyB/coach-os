using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Text.RegularExpressions;
using CoachOS.Infrastructure.Email;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace CoachOS.Tests.Services;

/// <summary>
/// Spamfilters (mail-tester: MIME_HTML_ONLY) straffen mails zonder text/plain-deel af.
/// Deze tests leggen de MIME-structuur vast: multipart/alternative met eerst text/plain
/// en als laatste een multipart/related (HTML + inline cid-logo).
/// </summary>
[TestFixture]
public class EmailMessageStructureTests
{
    private const string Html =
        "<html><head><style>p{margin:0}</style></head><body>"
        + "<img src=\"cid:coachos-logo\" alt=\"\" />"
        + "<p>Hoi Anna,</p><p><a href=\"https://coach-os.be/reset?t=abc\">Wachtwoord instellen</a></p>"
        + "</body></html>";

    private EmailService _sut = null!;
    private string _pickupDir = null!;

    [SetUp]
    public void SetUp()
    {
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
            new Mock<IMjmlTemplateRenderer>().Object,
            NullLogger<EmailService>.Instance);

        _pickupDir = Path.Combine(Path.GetTempPath(), "coachos-mail-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_pickupDir);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_pickupDir)) Directory.Delete(_pickupDir, recursive: true);
    }

    [Test]
    public void BuildMessage_HtmlBody_AddsPlainTextViewBeforeHtmlView()
    {
        using MailMessage message = _sut.BuildMessage("anna@b.be", "Anna", "Onderwerp", Html);

        message.AlternateViews.Should().HaveCount(2);
        message.AlternateViews[0].ContentType.MediaType.Should().Be(MediaTypeNames.Text.Plain);
        message.AlternateViews[1].ContentType.MediaType.Should().Be(MediaTypeNames.Text.Html);
    }

    [Test]
    public void BuildMessage_HtmlBody_PlainTextViewContainsConvertedText()
    {
        using MailMessage message = _sut.BuildMessage("anna@b.be", "Anna", "Onderwerp", Html);

        string plain = ReadView(message.AlternateViews[0]);

        plain.Should().Be(HtmlToPlainText.Convert(Html));
        plain.Should().Contain("Hoi Anna,");
        plain.Should().Contain("Wachtwoord instellen (https://coach-os.be/reset?t=abc)");
        plain.Should().NotContain("<").And.NotContain("margin");
    }

    [Test]
    public void BuildMessage_HtmlBody_LogoStaysLinkedToHtmlViewOnly()
    {
        using MailMessage message = _sut.BuildMessage("anna@b.be", "Anna", "Onderwerp", Html);

        message.AlternateViews[0].LinkedResources.Should().BeEmpty();
        message.AlternateViews[1].LinkedResources.Should().ContainSingle()
            .Which.ContentId.Should().Be("coachos-logo");
    }

    [Test]
    public void BuildMessage_SerializedMime_IsAlternativeWithPlainFirstAndRelatedHtmlLast()
    {
        using MailMessage message = _sut.BuildMessage("anna@b.be", "Anna", "Onderwerp", Html);

        string mime = Serialize(message);

        // Top-level: multipart/alternative.
        System.Text.RegularExpressions.Match top = Regex.Match(mime, @"^Content-Type:\s*multipart/alternative;\s*boundary=(""?)([^""\r\n]+)\1",
            RegexOptions.Multiline | RegexOptions.IgnoreCase);
        top.Success.Should().BeTrue(because: "de mail moet een multipart/alternative zijn");
        string boundary = top.Groups[2].Value;

        string[] parts = mime.Split("--" + boundary);
        // [0] = headers, [1] = plain, [2] = related, [3] = "--" afsluiter.
        parts.Length.Should().BeGreaterThanOrEqualTo(4);
        parts[1].Should().MatchRegex(@"(?i)Content-Type:\s*text/plain");
        parts[2].Should().MatchRegex(@"(?i)Content-Type:\s*multipart/related");
        parts[2].Should().MatchRegex(@"(?i)Content-Type:\s*text/html");
        parts[2].Should().MatchRegex(@"(?i)Content-Type:\s*image/png");
        parts[2].Should().MatchRegex(@"(?i)Content-ID:\s*<coachos-logo>");
    }

    private static string ReadView(AlternateView view)
    {
        view.ContentStream.Position = 0;
        using StreamReader reader = new(view.ContentStream, Encoding.UTF8, leaveOpen: true);
        return reader.ReadToEnd();
    }

    private string Serialize(MailMessage message)
    {
        using SmtpClient client = new()
        {
            DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory,
            PickupDirectoryLocation = _pickupDir,
        };
        client.Send(message);
        string file = Directory.GetFiles(_pickupDir, "*.eml").Single();
        return File.ReadAllText(file);
    }
}
