using CoachOS.Application.Camps.DTOs;
using CoachOS.Application.Camps.Validators;
using FluentAssertions;
using NUnit.Framework;

namespace CoachOS.Tests.Validators;

[TestFixture]
public class SaveCampPriceRequestValidatorTests
{
    private readonly SaveCampPriceRequestValidator _validator = new();

    [Test]
    public void Validate_ValidOption_Passes()
        => _validator.Validate(new SaveCampPriceRequest(null, "Leden", "Voor leden", 90m)).IsValid.Should().BeTrue();

    [Test]
    public void Validate_FreeOption_Passes()
        => _validator.Validate(new SaveCampPriceRequest(null, "Gratis", null, 0m)).IsValid.Should().BeTrue();

    [Test]
    public void Validate_EmptyLabel_Fails()
        => _validator.Validate(new SaveCampPriceRequest(null, " ", null, 90m)).IsValid.Should().BeFalse();

    [Test]
    public void Validate_NegativeAmount_Fails()
        => _validator.Validate(new SaveCampPriceRequest(null, "Leden", null, -1m)).IsValid.Should().BeFalse();

    [Test]
    public void Validate_TooLongDescription_Fails()
        => _validator.Validate(new SaveCampPriceRequest(null, "Leden", new string('x', 501), 5m)).IsValid.Should().BeFalse();

    [Test]
    public void CreateCampValidator_TooManyOptions_Fails()
    {
        List<SaveCampPriceRequest> prices = Enumerable.Range(0, 11)
            .Select(i => new SaveCampPriceRequest(null, $"Optie {i}", null, 10m)).ToList();
        CreateCampRequest request = new(
            "Kamp", null, Guid.NewGuid(), null, 100m, "2026-04-14", "2026-04-14",
            new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc), 20,
            [new("2026-04-14", "09:00", "16:00", [])], prices);

        new CreateCampRequestValidator().Validate(request).Errors
            .Should().Contain(e => e.ErrorMessage.Contains("maximaal"));
    }
}
