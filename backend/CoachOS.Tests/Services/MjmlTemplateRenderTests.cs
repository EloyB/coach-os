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
}
