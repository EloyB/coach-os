using CoachOS.Application.Auth.DTOs;
using CoachOS.Application.Auth.Validators;
using CoachOS.Application.SuperAdmin.DTOs;
using CoachOS.Application.SuperAdmin.Validators;
using FluentAssertions;
using NUnit.Framework;

namespace CoachOS.Tests.Validators;

[TestFixture]
public class VerifyEmailRequestValidatorTests
{
    private VerifyEmailRequestValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new VerifyEmailRequestValidator();

    [TestCase("123456")]
    [TestCase("000000")]
    public void Six_digit_code_passes(string code)
    {
        _validator.Validate(new VerifyEmailRequest(code)).IsValid.Should().BeTrue();
    }

    [TestCase("")]
    [TestCase("12345")]
    [TestCase("1234567")]
    [TestCase("12a456")]
    [TestCase("123 456")]
    public void Malformed_code_fails(string code)
    {
        _validator.Validate(new VerifyEmailRequest(code)).IsValid.Should().BeFalse();
    }
}

[TestFixture]
public class ExtendTrialRequestValidatorTests
{
    private ExtendTrialRequestValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new ExtendTrialRequestValidator();

    [TestCase(1)]
    [TestCase(14)]
    [TestCase(90)]
    public void Days_within_range_pass(int days)
    {
        _validator.Validate(new ExtendTrialRequest(days)).IsValid.Should().BeTrue();
    }

    [TestCase(0)]
    [TestCase(-5)]
    [TestCase(91)]
    public void Days_out_of_range_fail(int days)
    {
        _validator.Validate(new ExtendTrialRequest(days)).IsValid.Should().BeFalse();
    }
}
