using AwesomeAssertions;
using CoachOS.API.Extensions;
using CoachOS.Application.Configuration;
using NUnit.Framework;

namespace CoachOS.Tests.Common;

[TestFixture]
public class LegalAndSecretConfigurationTests
{
    [TestCase("algemene-voorwaarden-clubs.md")]
    [TestCase("verwerkersovereenkomst.md")]
    public void Vastgelegde_versie_komt_overeen_met_de_juridische_tekst(string document)
    {
        var path = Path.Combine(FindRepoRoot(), "docs", "legal", document);
        var versionLine = File.ReadLines(path).First(l => l.StartsWith("**Versie:**"));

        versionLine.Should().Be($"**Versie:** {LegalDocumentVersions.Current}");
    }

    [Test]
    public void Standaardversies_bij_registratie_zijn_niet_meer_concept()
    {
        var options = new RegistrationOptions();

        options.TermsVersion.Should().Be(LegalDocumentVersions.Current);
        options.DataProcessingAgreementVersion.Should().Be(LegalDocumentVersions.Current);
    }

    [Test]
    public void Placeholder_secret_telt_als_niet_ingevuld()
    {
        var normalized = ConfigurationExtensions.NormalizeScalewaySecrets(
        [
            new("Billit__ApiKey", ConfigurationExtensions.UnsetSecretPlaceholder),
            new("MollieBilling__ApiKey", "test_abc"),
            new("Jwt:Key", "ignored"),
        ]);

        normalized["Billit__ApiKey"].Should().BeEmpty();
        normalized["Billit:ApiKey"].Should().BeEmpty();
        normalized["MollieBilling:ApiKey"].Should().Be("test_abc");
        normalized.Should().NotContainKey("Jwt:Key");
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "docs", "legal")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new DirectoryNotFoundException("docs/legal niet gevonden.");
    }
}
