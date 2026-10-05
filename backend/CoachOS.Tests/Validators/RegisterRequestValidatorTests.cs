using CoachOS.Application.Auth.DTOs;
using CoachOS.Application.Auth.Validators;
using FluentAssertions;
using FluentValidation.Results;
using NUnit.Framework;

namespace CoachOS.Tests.Validators;

[TestFixture]
public class RegisterRequestValidatorTests
{
    private RegisterRequestValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new RegisterRequestValidator();

    private static RegisterRequest ValidRequest() => new()
    {
        OrganizationName = "TC Brederode",
        FirstName = "Jan",
        LastName = "Janssen",
        Email = "jan@brederode.be",
        Password = "Wachtwoord1",
        AcceptTerms = true,
        AcceptDataProcessingAgreement = true
    };

    [Test]
    public void Valid_request_passes()
    {
        ValidationResult result = _validator.Validate(ValidRequest());

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Terms_not_accepted_fails()
    {
        ValidationResult result = _validator.Validate(ValidRequest() with { AcceptTerms = false });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Be("Je moet akkoord gaan met de algemene voorwaarden");
    }

    [Test]
    public void Data_processing_agreement_not_accepted_fails()
    {
        ValidationResult result = _validator.Validate(ValidRequest() with { AcceptDataProcessingAgreement = false });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Be("Je moet akkoord gaan met de verwerkersovereenkomst");
    }

    [Test]
    public void Filled_honeypot_fails_with_generic_message()
    {
        ValidationResult result = _validator.Validate(ValidRequest() with { Website = "https://spam.example" });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Be("Registratie mislukt. Probeer het opnieuw.");
    }

    [TestCase(null)]
    [TestCase("")]
    public void Empty_honeypot_passes(string? website)
    {
        ValidationResult result = _validator.Validate(ValidRequest() with { Website = website });

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Weak_password_fails()
    {
        ValidationResult result = _validator.Validate(ValidRequest() with { Password = "zwak" });

        result.IsValid.Should().BeFalse();
    }
}
