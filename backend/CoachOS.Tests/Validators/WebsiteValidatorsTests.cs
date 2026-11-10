using CoachOS.Application.WebsiteContact.DTOs;
using CoachOS.Application.WebsiteContact.Validators;
using CoachOS.Application.WebsiteWaitlist.DTOs;
using CoachOS.Application.WebsiteWaitlist.Validators;
using FluentAssertions;
using FluentValidation.Results;
using NUnit.Framework;

namespace CoachOS.Tests.Validators;

[TestFixture]
public class WebsiteValidatorsTests
{
    private static SubmitWebsiteContactRequest ValidContact() =>
        new("Anna Peeters", "anna@example.test", "Graag meer info over CoachOS.", "TC Brederode");

    [Test]
    public void Contact_ValidRequest_Passes() =>
        new SubmitWebsiteContactValidator().Validate(ValidContact()).IsValid.Should().BeTrue();

    [Test]
    public void Contact_WithoutOrganization_Passes() =>
        new SubmitWebsiteContactValidator().Validate(ValidContact() with { Organization = null }).IsValid.Should().BeTrue();

    [TestCase("Name", "A", "Naam moet minstens 2 karakters zijn")]
    [TestCase("Email", "geen-mail", "Ongeldig e-mailadres")]
    [TestCase("Message", "kort", "Bericht moet minstens 10 karakters zijn")]
    [TestCase("Organization", null, "Organisatie mag maximaal 100 karakters zijn")]
    public void Contact_InvalidField_FailsWithDutchMessage(string field, string? value, string message)
    {
        SubmitWebsiteContactRequest request = field switch
        {
            "Name" => ValidContact() with { Name = value! },
            "Email" => ValidContact() with { Email = value! },
            "Message" => ValidContact() with { Message = value! },
            _ => ValidContact() with { Organization = new string('x', 101) },
        };

        ValidationResult result = new SubmitWebsiteContactValidator().Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == field && e.ErrorMessage == message);
    }

    [TestCase("bad", "club", false)]
    [TestCase("safe@example.test", "https://attacker.invalid", false)]
    [TestCase("safe@example.test", "anders", true)]
    [TestCase("safe@example.test", null, true)]
    public void Waitlist_RestrictsPayload(string email, string? role, bool valid) =>
        new RegisterWebsiteWaitlistValidator().Validate(new RegisterWebsiteWaitlistRequest(email, role)).IsValid.Should().Be(valid);

    [Test]
    public void Waitlist_InvalidRole_HasDutchMessage() =>
        new RegisterWebsiteWaitlistValidator().Validate(new RegisterWebsiteWaitlistRequest("a@example.test", "x"))
            .Errors.Should().ContainSingle(e => e.ErrorMessage == "Ongeldige rol");

    [TestCase("guess", false)]
    [TestCase("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA", false)]
    [TestCase("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", true)]
    public void Unsubscribe_AcceptsOnlyLowercaseHexToken(string token, bool valid) =>
        new UnsubscribeWebsiteWaitlistValidator().Validate(new UnsubscribeWebsiteWaitlistRequest(token)).IsValid.Should().Be(valid);
}
