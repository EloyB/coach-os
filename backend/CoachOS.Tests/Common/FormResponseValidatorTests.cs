using CoachOS.Application.Common;
using CoachOS.Domain.Models;
using FluentAssertions;
using NUnit.Framework;

namespace CoachOS.Tests.Common;

[TestFixture]
public class FormResponseValidatorTests
{
    private static (Guid Id, bool IsRequired, string Label, IReadOnlyList<string>? AllowedValues) Field(
        Guid id, bool required, string label, IReadOnlyList<string>? allowed = null)
        => (id, required, label, allowed);

    [Test]
    public void Validate_NoForm_ReturnsNull()
    {
        Error? error = FormResponseValidator.Validate(
            new List<(Guid, bool, string, IReadOnlyList<string>?)>(),
            new List<(Guid, string)>());
        error.Should().BeNull();
    }

    [Test]
    public void Validate_UnknownField_ReturnsValidationError()
    {
        Guid known = Guid.NewGuid();
        Error? error = FormResponseValidator.Validate(
            new[] { Field(known, false, "Allergieën") },
            new[] { (Guid.NewGuid(), "iets") });
        error.Should().NotBeNull();
        error!.Code.Should().Be(ErrorCodes.Validation);
        error.Message.Should().Be("Ongeldig formulierveld.");
    }

    [Test]
    public void Validate_MissingRequired_ReturnsFieldSpecificError()
    {
        Guid req = Guid.NewGuid();
        Error? error = FormResponseValidator.Validate(
            new[] { Field(req, true, "Allergieën") },
            new List<(Guid, string)>());
        error.Should().NotBeNull();
        error!.Message.Should().Be("Veld 'Allergieën' is verplicht.");
    }

    [Test]
    public void Validate_RequiredPresentAndKnown_ReturnsNull()
    {
        Guid req = Guid.NewGuid();
        Error? error = FormResponseValidator.Validate(
            new[] { Field(req, true, "Allergieën") },
            new[] { (req, "geen") });
        error.Should().BeNull();
    }

    [Test]
    public void Validate_RequiredButWhitespace_ReturnsError()
    {
        Guid req = Guid.NewGuid();
        Error? error = FormResponseValidator.Validate(
            new[] { Field(req, true, "Allergieën") },
            new[] { (req, "   ") });
        error.Should().NotBeNull();
    }

    // ── Choice-value validation (MultipleChoice / AgeCategory) ────────────────

    [Test]
    public void Validate_ChoiceValueInOptions_ReturnsNull()
    {
        Guid age = Guid.NewGuid();
        Error? error = FormResponseValidator.Validate(
            new[] { Field(age, true, "Leeftijdscategorie", new[] { "8-10 jaar", "Volwassenen" }) },
            new[] { (age, "Volwassenen") });
        error.Should().BeNull();
    }

    [Test]
    public void Validate_ChoiceValueNotInOptions_ReturnsValidationError()
    {
        Guid age = Guid.NewGuid();
        Error? error = FormResponseValidator.Validate(
            new[] { Field(age, true, "Leeftijdscategorie", new[] { "8-10 jaar", "Volwassenen" }) },
            new[] { (age, "verzonnen-bucket") });
        error.Should().NotBeNull();
        error!.Code.Should().Be(ErrorCodes.Validation);
        error.Message.Should().Be("Ongeldige keuze voor veld 'Leeftijdscategorie'.");
    }

    [Test]
    public void Validate_OptionalChoiceLeftEmpty_ReturnsNull()
    {
        // An unanswered optional choice field must not trip the option check.
        Guid choice = Guid.NewGuid();
        Error? error = FormResponseValidator.Validate(
            new[] { Field(choice, false, "Niveau", new[] { "Beginner", "Gevorderd" }) },
            new[] { (choice, "") });
        error.Should().BeNull();
    }
}
