using CoachOS.Application.SuperAdmin.DTOs;
using CoachOS.Application.SuperAdmin.Validators;
using FluentAssertions;
using NUnit.Framework;

namespace CoachOS.Tests.Validators;

[TestFixture]
public class CreateAdminRequestValidatorTests
{
    private CreateAdminRequestValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new CreateAdminRequestValidator();

    [Test]
    public void Valid_request_passes()
    {
        var request = new CreateAdminRequest
        {
            OrganizationName = "TC Brederode",
            FirstName = "Jan",
            LastName = "Janssen",
            Email = "jan@brederode.be",
            IsEarlyBird = true
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Empty_organization_name_fails()
    {
        var request = new CreateAdminRequest
        {
            OrganizationName = "",
            FirstName = "Jan",
            LastName = "Janssen",
            Email = "jan@brederode.be"
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAdminRequest.OrganizationName));
    }

    [Test]
    public void Invalid_email_fails()
    {
        var request = new CreateAdminRequest
        {
            OrganizationName = "TC Brederode",
            FirstName = "Jan",
            LastName = "Janssen",
            Email = "not-an-email"
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAdminRequest.Email));
    }

    [Test]
    public void Empty_first_or_last_name_fails()
    {
        var request = new CreateAdminRequest
        {
            OrganizationName = "TC Brederode",
            FirstName = "",
            LastName = "",
            Email = "jan@brederode.be"
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Select(e => e.PropertyName).Should().Contain(new[]
        {
            nameof(CreateAdminRequest.FirstName),
            nameof(CreateAdminRequest.LastName)
        });
    }
}
