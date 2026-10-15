using CoachOS.Infrastructure.Email;
using FluentAssertions;
using NUnit.Framework;

namespace CoachOS.Tests.Services;

/// <summary>
/// De text/plain-variant van transactionele mails wordt uit de gerenderde HTML
/// afgeleid. Deze tests leggen vast wat er moet verdwijnen (head, style, MSO-comments,
/// tags) en hoe links en blokelementen in platte tekst terechtkomen.
/// </summary>
[TestFixture]
public class HtmlToPlainTextTests
{
    [Test]
    public void Convert_EmptyInput_ReturnsEmptyString()
    {
        string result = HtmlToPlainText.Convert(string.Empty);

        result.Should().BeEmpty();
    }

    [Test]
    public void Convert_HeadAndStyle_AreRemoved()
    {
        string html = "<html><head><title>Titel</title><style>.x { color: red; }</style></head>"
            + "<body><style>p { margin: 0; }</style><p>Hallo</p></body></html>";

        string result = HtmlToPlainText.Convert(html);

        result.Should().Be("Hallo");
    }

    [Test]
    public void Convert_MsoConditionalComments_AreRemovedButNonMsoContentIsKept()
    {
        string html = "<!--[if mso | IE]><table><tr><td>Alleen Outlook</td></tr></table><![endif]-->"
            + "<!--[if !mso]><!--><div>Voor iedereen</div><!--<![endif]-->";

        string result = HtmlToPlainText.Convert(html);

        result.Should().NotContain("Alleen Outlook");
        result.Should().Contain("Voor iedereen");
        result.Should().NotContain("<!--").And.NotContain("endif");
    }

    [Test]
    public void Convert_LinkWithDifferentLabel_RendersLabelWithUrlInParentheses()
    {
        string html = "<p>Klik <a href=\"https://coach-os.be/reset?t=1\" style=\"color:#2D5016\">hier</a> om verder te gaan.</p>";

        string result = HtmlToPlainText.Convert(html);

        result.Should().Be("Klik hier (https://coach-os.be/reset?t=1) om verder te gaan.");
    }

    [Test]
    public void Convert_LinkWhoseLabelIsTheUrl_RendersUrlOnce()
    {
        string html = "<a href=\"https://coach-os.be/x\">https://coach-os.be/x</a>";

        string result = HtmlToPlainText.Convert(html);

        result.Should().Be("https://coach-os.be/x");
    }

    [Test]
    public void Convert_LinkWithEncodedAmpersand_DecodesUrlExactlyOnce()
    {
        string html = "<a href=\"https://coach-os.be/c?a=1&amp;b=2\">Bevestigen</a>";

        string result = HtmlToPlainText.Convert(html);

        result.Should().Be("Bevestigen (https://coach-os.be/c?a=1&b=2)");
    }

    [Test]
    public void Convert_LinkWithNestedTagsInLabel_UsesTextOfLabel()
    {
        string html = "<a href=\"https://coach-os.be\"><span style=\"x\">Coach</span><b>OS</b></a>";

        string result = HtmlToPlainText.Convert(html);

        result.Should().Be("CoachOS (https://coach-os.be)");
    }

    [Test]
    public void Convert_LinkWithoutLabel_RendersUrl()
    {
        string html = "<a href=\"https://coach-os.be/logo\"><img src=\"cid:coachos-logo\" alt=\"\" /></a>";

        string result = HtmlToPlainText.Convert(html);

        result.Should().Be("https://coach-os.be/logo");
    }

    [Test]
    public void Convert_BlockElementsAndBreaks_BecomeLineBreaks()
    {
        string html = "<h1>Titel</h1><p>Eerste alinea</p><div>Regel 1<br />Regel 2<br>Regel 3</div>"
            + "<table><tr><td>Rij 1</td></tr><tr><td>Rij 2</td></tr></table><h3>Slot</h3>";

        string result = HtmlToPlainText.Convert(html);

        string[] lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines.Should().Equal("Titel", "Eerste alinea", "Regel 1", "Regel 2", "Regel 3", "Rij 1", "Rij 2", "Slot");
    }

    [Test]
    public void Convert_HtmlEntities_AreDecoded()
    {
        string html = "<p>Caf&eacute; &amp; co &mdash; &euro; 12,50&nbsp;per les &#8212; &lt;tag&gt;</p>";

        string result = HtmlToPlainText.Convert(html);

        result.Should().Be("Café & co — € 12,50 per les — <tag>");
    }

    [Test]
    public void Convert_Whitespace_IsCollapsedAndBlankLinesLimited()
    {
        string html = "<div>\n    Hoi   Anna,\n\t\t</div>\n\n\n<div></div><div></div><div></div><p>   Tot   snel  </p>";

        string result = HtmlToPlainText.Convert(html);

        result.Should().Be("Hoi Anna,\n\nTot snel");
    }

    [Test]
    public void Convert_Output_UsesNoCarriageReturnsOrTags()
    {
        string html = "<p>Lijn\r\nmet CRLF</p><span>en</span> <strong>tekst</strong>";

        string result = HtmlToPlainText.Convert(html);

        result.Should().NotContain("\r").And.NotContain("<").And.NotContain(">");
        result.Should().Be("Lijn met CRLF\nen tekst");
    }
}
