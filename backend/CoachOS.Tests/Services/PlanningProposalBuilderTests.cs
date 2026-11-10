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

    [Test]
    public void GetSharedAgeCategory_MemberWithWhitespaceAnswer_ReturnsNull()
    {
        // A whitespace-only response must count as "no answer", not a distinct bucket.
        var members = new List<Enrollment> { MemberWithAge("8-10 jaar"), MemberWithAge("   ") };

        PlanningProposalBuilder.GetSharedAgeCategory(members).Should().BeNull();
    }

    [Test]
    public void GetSharedAgeCategory_PaddedAnswersMatch_ReturnsTrimmedBucket()
    {
        // Surrounding whitespace is normalized so equal buckets still agree.
        var members = new List<Enrollment> { MemberWithAge(" 8-10 jaar "), MemberWithAge("8-10 jaar") };

        PlanningProposalBuilder.GetSharedAgeCategory(members).Should().Be("8-10 jaar");
    }

    // ── BuildUnits: ambiguous-age groups are ineligible for automatic grouping ────

    private static EnrollmentUnit BuildOpenGroupUnit(params string?[] memberBuckets)
    {
        Guid groupId = Guid.NewGuid();
        var members = new List<Enrollment>();
        foreach (string? bucket in memberBuckets)
        {
            Enrollment e = MemberWithAge(bucket);
            e.EnrollmentGroupId = groupId;
            e.IsOpenToGrouping = true;
            members.Add(e);
        }
        var group = new EnrollmentGroup { Id = groupId, LeaderEnrollmentId = members[0].Id };

        (List<EnrollmentUnit> units, _) = PlanningProposalBuilder.BuildUnits(
            members, new[] { group }, new(), new(), new());
        return units.Single(u => u.IsGroup);
    }

    [Test]
    public void BuildUnits_OpenGroupWithMissingMemberAge_IsForcedExclusive()
    {
        // Regression: an open group whose age bucket is ambiguous (a member didn't answer) must
        // NOT be auto-groupable — otherwise it could be merged into a slot of a different bucket.
        EnrollmentUnit unit = BuildOpenGroupUnit("4-6", null);

        unit.IsOpenToGrouping.Should().BeFalse();
        unit.AgeCategory.Should().BeNull();
    }

    [Test]
    public void BuildUnits_OpenGroupWithMixedMemberAges_IsForcedExclusive()
    {
        EnrollmentUnit unit = BuildOpenGroupUnit("4-6", "9-11");

        unit.IsOpenToGrouping.Should().BeFalse();
    }

    [Test]
    public void BuildUnits_OpenGroupAllSameAge_StaysOpenToGrouping()
    {
        EnrollmentUnit unit = BuildOpenGroupUnit("4-6", "4-6");

        unit.IsOpenToGrouping.Should().BeTrue();
        unit.AgeCategory.Should().Be("4-6");
    }

    [Test]
    public void BuildUnits_OpenGroupWithNoAgeField_StaysOpenToGrouping()
    {
        // Feature off (nobody answered): behaves exactly as before — unconstrained and open.
        EnrollmentUnit unit = BuildOpenGroupUnit(null, null);

        unit.IsOpenToGrouping.Should().BeTrue();
        unit.AgeCategory.Should().BeNull();
    }
}
