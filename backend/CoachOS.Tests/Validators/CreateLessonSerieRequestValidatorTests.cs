using CoachOS.Application.LessonSerie.DTOs;
using CoachOS.Application.LessonSerie.Validators;
using FluentAssertions;
using FluentValidation.Results;
using FluentValidation.TestHelper;
using NUnit.Framework;

namespace CoachOS.Tests.Validators;

[TestFixture]
public class CreateLessonSerieRequestValidatorTests
{
    private CreateLessonSerieRequestValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new CreateLessonSerieRequestValidator();

    private static CreateLessonSerieRequest ValidRequest() => new()
    {
        Name = "Kids Tennis",
        TennisClubId = Guid.NewGuid(),
        Price = 100m,
        StartDate = "2026-08-01",
        EndDate = "2026-09-01",
        RegistrationDeadline = new DateTime(2026, 7, 25),
        Lessons = new()
        {
            new()
            {
                Date = "2026-08-01",
                StartTime = "10:00",
                EndTime = "11:00",
                MaxStudents = 4,
            },
        },
    };

    [Test]
    public void ValidRequest_Passes()
    {
        ValidationResult result = _validator.Validate(ValidRequest());

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void MinAgeGreaterThanMaxAge_Fails()
    {
        CreateLessonSerieRequest request = ValidRequest() with { MinAge = 50, MaxAge = 10 };

        _validator.Validate(request).Errors
            .Should().Contain(e => e.ErrorMessage == "Minimumleeftijd mag niet groter zijn dan de maximumleeftijd.");
    }

    [Test]
    public void AgeBounds_0_And_120_Pass()
    {
        CreateLessonSerieRequest request = ValidRequest() with { MinAge = 0, MaxAge = 120 };

        _validator.Validate(request).Errors
            .Should().NotContain(e => e.PropertyName == "MinAge" || e.PropertyName == "MaxAge");
    }

    [Test]
    public void MaxAgeAbove120_Fails()
    {
        CreateLessonSerieRequest request = ValidRequest() with { MaxAge = 121 };

        _validator.Validate(request).IsValid.Should().BeFalse();
    }

    [Test]
    public void Fails_when_no_enrollment_mode_selected()
    {
        CreateLessonSerieRequest req = ValidRequest() with
        {
            AllowSoloEnrollment = false,
            AllowGroupEnrollment = false,
        };
        TestValidationResult<CreateLessonSerieRequest> result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.AllowSoloEnrollment);
    }

    [Test]
    public void Fails_when_no_payment_method_selected()
    {
        CreateLessonSerieRequest req = ValidRequest() with
        {
            AcceptOnlinePayment = false,
            AcceptManualPayment = false,
        };
        TestValidationResult<CreateLessonSerieRequest> result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.AcceptOnlinePayment);
    }

    [Test]
    public void Passes_with_solo_only_and_manual_only()
    {
        CreateLessonSerieRequest req = ValidRequest() with
        {
            AllowSoloEnrollment = true, AllowGroupEnrollment = false,
            AcceptOnlinePayment = false, AcceptManualPayment = true,
        };
        _validator.TestValidate(req).ShouldNotHaveAnyValidationErrors();
    }
}
