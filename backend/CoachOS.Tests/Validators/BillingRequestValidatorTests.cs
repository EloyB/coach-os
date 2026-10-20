using CoachOS.Application.Billing.DTOs;
using CoachOS.Application.Billing.Validators;
using FluentAssertions;
using FluentValidation.Results;
using NUnit.Framework;

namespace CoachOS.Tests.Validators;

[TestFixture]
public class BillingRequestValidatorTests
{
    private static SaveBillingProfileRequest Valid() =>
        new("TC Brederode vzw", "Molenstraat 1", "2000", "Antwerpen", "BE", "f@brederode.be", "BE0123456789");

    private static ValidationResult Validate(SaveBillingProfileRequest r) => new SaveBillingProfileRequestValidator().Validate(r);

    [Test]
    public void ValidProfile_Passes() => Validate(Valid()).IsValid.Should().BeTrue();

    [Test]
    public void VatNumberOptional_Passes() => Validate(Valid() with { VatNumber = null }).IsValid.Should().BeTrue();

    [TestCase("", "Street")]
    [TestCase("x", "PostalCode")]
    [TestCase("x", "InvoiceEmail")]
    [TestCase("x", "VatNumber")]
    public void InvalidField_Fails(string value, string field)
    {
        SaveBillingProfileRequest request = field switch
        {
            "Street" => Valid() with { Street = value },
            "PostalCode" => Valid() with { PostalCode = value },
            "InvoiceEmail" => Valid() with { InvoiceEmail = value },
            _ => Valid() with { VatNumber = value },
        };

        ValidationResult result = Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == field);
    }

    [Test]
    public void EmptyName_Fails() => Validate(Valid() with { Name = " " }).IsValid.Should().BeFalse();

    [TestCase(1, 1, 1, true)]
    [TestCase(3, 2, 2, true)]
    [TestCase(4, 1, 1, false)]
    [TestCase(1, 3, 1, false)]
    [TestCase(1, 1, 3, false)]
    public void Checkout_ValidatesEnumValues(int plan, int interval, int method, bool valid)
        => new CheckoutRequestValidator().Validate(new CheckoutRequest(plan, interval, method)).IsValid.Should().Be(valid);
}
