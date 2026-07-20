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

    private static SubmitEnrollmentRequest ValidGroup() => new()
    {
        StudentName = "Leader",
        StudentEmail = "leader@test.be",
        EnrollmentType = "group",
        GroupMembers = new()
        {
            new() { StudentName = "Member A", StudentEmail = "a@test.be" },
            new() { StudentName = "Member B", StudentEmail = "b@test.be" },
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
            EnrollmentType = "solo",
        };

        ValidationResult result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}
