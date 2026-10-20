using CoachOS.Domain.Subscriptions;
using FluentAssertions;
using NUnit.Framework;

namespace CoachOS.Tests.DomainLogic;

[TestFixture]
public class BelgianVatTests
{
    [TestCase(25, 30.25)]
    [TestCase(49, 59.29)]
    [TestCase(890, 1076.90)]
    [TestCase(0, 0)]
    public void Gross_AddsTwentyOnePercentRoundedToCents(decimal net, decimal expected)
        => BelgianVat.Gross(net).Should().Be(expected);

    [TestCase("BE0123456789", true)]
    [TestCase("0123.456.789", true)]
    [TestCase("BE 1234 567 890", true)]
    [TestCase("be0123456789", true)]
    [TestCase("BE2123456789", false)]
    [TestCase("BE012345678", false)]
    [TestCase("NL123456789B01", false)]
    [TestCase("", false)]
    public void IsValidFormat(string input, bool expected)
        => BelgianVat.IsValidFormat(input).Should().Be(expected);

    [TestCase("0123.456.789", "BE0123456789")]
    [TestCase("be 1234 567 890", "BE1234567890")]
    public void Normalize_ReturnsCanonicalForm(string input, string expected)
        => BelgianVat.Normalize(input).Should().Be(expected);
}
