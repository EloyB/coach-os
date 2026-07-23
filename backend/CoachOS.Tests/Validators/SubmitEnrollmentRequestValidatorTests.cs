using CoachOS.Application.Enrollments.DTOs;
using CoachOS.Application.Enrollments.Validators;
using FluentAssertions;
using FluentValidation.Results;
using NUnit.Framework;

namespace CoachOS.Tests.Validators;

[TestFixture]
public class SubmitEnrollmentRequestValidatorTests
{
    private const string DuplicateEmailMessage = "Elk groepslid moet een uniek e-mailadres hebben";

    private SubmitEnrollmentRequestValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new SubmitEnrollmentRequestValidator();

    private const string AdultBirthDate = "1990-05-12";
    private const string YouthBirthDate = "2012-05-12";

    private static SubmitEnrollmentRequest ValidGroup() => new()
    {
        StudentName = "Leader",
        StudentEmail = "leader@test.be",
        DateOfBirth = AdultBirthDate,
        EnrollmentType = "group",
        GroupMembers = new()
        {
            new() { StudentName = "Member A", StudentEmail = "a@test.be", DateOfBirth = AdultBirthDate },
            new() { StudentName = "Member B", StudentEmail = "b@test.be", DateOfBirth = YouthBirthDate },
        },
    };

    [Test]
    public void Validate_GroupWithUniqueEmails_Passes()
    {
        ValidationResult result = _validator.Validate(ValidGroup());
        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_MemberEmailEqualsLeaderEmail_Fails()
    {
        SubmitEnrollmentRequest request = ValidGroup() with
        {
            GroupMembers = new()
            {
                new() { StudentName = "Member A", StudentEmail = "leader@test.be" },
            },
        };

        ValidationResult result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == DuplicateEmailMessage);
    }

    [Test]
    public void Validate_TwoMembersShareEmail_Fails()
    {
        SubmitEnrollmentRequest request = ValidGroup() with
        {
            GroupMembers = new()
            {
                new() { StudentName = "Member A", StudentEmail = "same@test.be" },
                new() { StudentName = "Member B", StudentEmail = "same@test.be" },
            },
        };

        ValidationResult result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == DuplicateEmailMessage);
    }

    [Test]
    public void Validate_DuplicateEmailDifferentCasingAndWhitespace_Fails()
    {
        // De DB-index is hoofdlettergevoelig, maar functioneel is dit hetzelfde adres.
        SubmitEnrollmentRequest request = ValidGroup() with
        {
            GroupMembers = new()
            {
                new() { StudentName = "Member A", StudentEmail = " Leader@Test.be " },
            },
        };

        ValidationResult result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == DuplicateEmailMessage);
    }

    [Test]
    public void Validate_SoloEnrollment_SkipsGroupEmailCheck()
    {
        SubmitEnrollmentRequest request = new()
        {
            StudentName = "Anna",
            StudentEmail = "anna@test.be",
            DateOfBirth = AdultBirthDate,
            EnrollmentType = "solo",
        };

        ValidationResult result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    // ── Geboortedatum ─────────────────────────────────────────────────────────

    [Test]
    public void Validate_MissingDateOfBirth_Fails()
    {
        SubmitEnrollmentRequest request = ValidGroup() with { DateOfBirth = "" };

        ValidationResult result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Geboortedatum is verplicht");
    }

    [Test]
    public void Validate_MalformedDateOfBirth_Fails()
    {
        SubmitEnrollmentRequest request = ValidGroup() with { DateOfBirth = "12/05/1990" };

        ValidationResult result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.ErrorMessage == "Geboortedatum moet het formaat yyyy-MM-dd hebben");
    }

    [Test]
    public void Validate_FutureDateOfBirth_Fails()
    {
        string future = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)).ToString("yyyy-MM-dd");
        SubmitEnrollmentRequest request = ValidGroup() with { DateOfBirth = future };

        ValidationResult result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.ErrorMessage == "Geboortedatum kan niet in de toekomst liggen");
    }

    [Test]
    public void Validate_UnrealisticallyOldDateOfBirth_Fails()
    {
        // Typfout in het jaartal (1090 i.p.v. 1990) mag niet doorglippen.
        SubmitEnrollmentRequest request = ValidGroup() with { DateOfBirth = "1090-05-12" };

        ValidationResult result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Controleer de geboortedatum");
    }

    [Test]
    public void Validate_GroupMemberMissingDateOfBirth_Fails()
    {
        SubmitEnrollmentRequest request = ValidGroup() with
        {
            GroupMembers = new()
            {
                new() { StudentName = "Member A", StudentEmail = "a@test.be", DateOfBirth = "" },
            },
        };

        ValidationResult result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Geboortedatum is verplicht");
    }
}
