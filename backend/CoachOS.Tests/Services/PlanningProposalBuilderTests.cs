using CoachOS.Application.Planning;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using FluentAssertions;
using NUnit.Framework;

namespace CoachOS.Tests.Services;

[TestFixture]
public class PlanningProposalBuilderTests
{
    private static Enrollment MemberWithAge(string? bucket)
    {
        var enrollment = new Enrollment { Id = Guid.NewGuid(), StudentName = "Member" };
        if (bucket is not null)
            enrollment.FormResponses.Add(new FormResponse
            {
                FormField = new FormField { Type = FormFieldType.AgeCategory },
                Value = bucket,
            });
        return enrollment;
    }

    [Test]
    public void GetSharedAgeCategory_AllMembersSameBucket_ReturnsBucket()
    {
        var members = new List<Enrollment> { MemberWithAge("8-10 jaar"), MemberWithAge("8-10 jaar") };

        PlanningProposalBuilder.GetSharedAgeCategory(members).Should().Be("8-10 jaar");
    }

    [Test]
    public void GetSharedAgeCategory_MembersDifferentBuckets_ReturnsNull()
    {
        var members = new List<Enrollment> { MemberWithAge("8-10 jaar"), MemberWithAge("Volwassenen") };

        PlanningProposalBuilder.GetSharedAgeCategory(members).Should().BeNull();
    }

    [Test]
    public void GetSharedAgeCategory_OneMemberMissingAnswer_ReturnsNull()
    {
        // Regression: a missing bucket must not be filtered away, leaving a single "agreed"
        // value — the group stays unconstrained instead of being locked to a partial answer.
        var members = new List<Enrollment> { MemberWithAge("8-10 jaar"), MemberWithAge(null) };

        PlanningProposalBuilder.GetSharedAgeCategory(members).Should().BeNull();
    }

    [Test]
    public void GetSharedAgeCategory_NoMemberAnswered_ReturnsNull()
    {
        var members = new List<Enrollment> { MemberWithAge(null), MemberWithAge(null) };

        PlanningProposalBuilder.GetSharedAgeCategory(members).Should().BeNull();
    }
}
