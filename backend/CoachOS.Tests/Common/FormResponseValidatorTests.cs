using CoachOS.Application.Common;
using CoachOS.Domain.Models;
using FluentAssertions;
using NUnit.Framework;

namespace CoachOS.Tests.Common;

[TestFixture]
public class FormResponseValidatorTests
{
    private static (Guid Id, bool IsRequired, string Label) Field(Guid id, bool required, string label) => (id, required, label);

    [Test]
    public void Validate_NoForm_ReturnsNull()
    {
        Error? error = FormResponseValidator.Validate(
            new List<(Guid, bool, string)>(),
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
}
