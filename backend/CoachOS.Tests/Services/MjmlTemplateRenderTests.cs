using CoachOS.Infrastructure.Email;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace CoachOS.Tests.Services;

/// <summary>
/// Bewijst dat alle MJML-templates compileren (de renderer gooit anders bij constructie)
/// en, specifiek voor de gebundelde planningsmail, dat de raw-placeholder de MJML-
/// compilatie overleeft. Kale tekst in een mj-column wordt door MJML weggegooid; de
/// placeholder moet dus in mj-raw staan, anders verdwijnen de deelnemersblokken stil.
/// </summary>
[TestFixture]
public class MjmlTemplateRenderTests
{
    private MjmlTemplateRenderer _renderer = null!;

    [SetUp]
    public void SetUp()
    {
        // De constructor compileert élke template en werpt bij een MJML-fout.
        _renderer = new MjmlTemplateRenderer(NullLogger<MjmlTemplateRenderer>.Instance);
    }

    [Test]
    public void ScheduleConfirmationMulti_KeepsParticipantBlocksPlaceholder()
    {
        string html = _renderer.Render("schedule-confirmation-multi", new Dictionary<string, string>
        {
            ["seriesName"] = "Voorjaar",
            ["participantNames"] = "Lotte, Sofie",
            ["raw:participantBlocks"] = "<div id=\"participant-marker\">blok</div>",
            ["year"] = "2026",
        });

        html.Should().Contain("participant-marker",
            because: "de raw-placeholder moet de MJML-compilatie overleven, anders verdwijnen de deelnemersblokken");
        html.Should().Contain("Voorjaar");
    }

    [Test]
    public void EmailVerification_RendersCodeAndValidity()
    {
        string html = _renderer.Render("email-verification", new Dictionary<string, string>
        {
            ["firstName"] = "Jan",
            ["code"] = "042917",
            ["validMinutes"] = "30",
            ["year"] = "2026",
        });

        html.Should().Contain("042917");
        html.Should().Contain("Hoi Jan");
        html.Should().Contain("30 minuten");
        html.Should().NotContain("{{");
    }

    [Test]
    public void TrialReminder_RendersHeadingBodyAndLink()
    {
        string html = _renderer.Render("trial-reminder", new Dictionary<string, string>
        {
            ["organizationName"] = "TC Brederode",
            ["heading"] = "Nog 10 dagen proef",
            ["body"] = "Je gratis proef loopt nog tot en met 04/11/2026.",
            ["appUrl"] = "https://app.coach-os.be/dashboard",
            ["year"] = "2026",
        });

        html.Should().Contain("Nog 10 dagen proef");
        html.Should().Contain("TC Brederode");
        html.Should().Contain("04/11/2026");
        html.Should().Contain("https://app.coach-os.be/dashboard");
        html.Should().NotContain("{{");
    }
}
