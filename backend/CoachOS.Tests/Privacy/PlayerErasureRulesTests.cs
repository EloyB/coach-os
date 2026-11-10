using CoachOS.Application.Privacy;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Models.Privacy;
using FluentAssertions;
using NUnit.Framework;
using static CoachOS.Tests.Privacy.PlayerErasureTestData;

namespace CoachOS.Tests.Privacy;

[TestFixture]
public class PlayerErasureRulesTests
{
    [Test]
    public void Evaluate_OldParticipationWithoutPayments_CanErase()
    {
        PlayerErasureEvaluation result = PlayerErasureRules.Evaluate(Facts(), Today);

        result.CanErase.Should().BeTrue();
        result.GroupEffect.Should().Be(ErasureGroupEffect.None);
    }

    [Test]
    public void Evaluate_AnyPaymentRecord_BlocksBecauseFinancialRecordsMustBeKept()
    {
        PlayerErasureRules.Evaluate(Facts(hasPayments: true), Today).BlockingReasons
            .Should().Equal(PlayerErasureBlockReasons.FinancialRecords);
    }

    [TestCase(EnrollmentStatus.PendingPayment, false)]
    [TestCase(EnrollmentStatus.Confirmed, true)]
    public void Evaluate_OpenPayment_Blocks(EnrollmentStatus status, bool openPaymentRow)
    {
        PlayerErasureRules.Evaluate(Facts(status: status, openPayments: openPaymentRow), Today).BlockingReasons
            .Should().Contain(PlayerErasureBlockReasons.OpenPayment);
    }

    [Test]
    public void Evaluate_FutureOrTodayParticipation_Blocks()
    {
        PlayerErasureRules.Evaluate(Facts(lastParticipation: Today), Today).BlockingReasons
            .Should().Contain(PlayerErasureBlockReasons.ActiveOrFutureParticipation);
    }

    [Test]
    public void Evaluate_UnknownParticipationOrBrokenChain_Blocks()
    {
        PlayerErasureRules.Evaluate(Facts(noParticipation: true), Today).BlockingReasons
            .Should().Contain(PlayerErasureBlockReasons.ParticipationUnknown);
        PlayerErasureRules.Evaluate(Facts(chainIncomplete: true), Today).BlockingReasons
            .Should().Contain(PlayerErasureBlockReasons.ParticipationChainIncomplete);
    }

    [Test]
    public void Evaluate_PendingMail_Blocks()
    {
        PlayerErasureRules.Evaluate(Facts(pendingMail: true), Today).BlockingReasons
            .Should().Contain(PlayerErasureBlockReasons.PendingMail);
    }

    [Test]
    public void Evaluate_GroupEffects_DependOnLeadershipAndSize()
    {
        Guid target = Guid.NewGuid();
        Guid other = Guid.NewGuid();

        PlayerErasureRules.Evaluate(Facts(target, group: Group(other, target, other)), Today).GroupEffect
            .Should().Be(ErasureGroupEffect.LeaveGroup);
        PlayerErasureRules.Evaluate(Facts(target, group: Group(target, target)), Today).GroupEffect
            .Should().Be(ErasureGroupEffect.RemoveEmptyGroup);
        PlayerErasureEvaluation transfer = PlayerErasureRules.Evaluate(Facts(target, group: Group(target, target, other)), Today);
        transfer.GroupEffect.Should().Be(ErasureGroupEffect.TransferLeader);
        transfer.OtherGroupMembers.Select(m => m.Id).Should().Equal(other);
        transfer.CanErase.Should().BeTrue("the admin chooses the new leader in the confirmation step");
    }

    [Test]
    public void Evaluate_GroupWithoutItsLeaderOrWithPayments_Blocks()
    {
        Guid target = Guid.NewGuid();
        PlayerErasureRules.Evaluate(Facts(target, group: Group(Guid.NewGuid(), target)), Today).BlockingReasons
            .Should().Contain(PlayerErasureBlockReasons.GroupInconsistent);
        ErasureGroupFacts paid = Group(target, target) with { HasPayments = true };
        PlayerErasureRules.Evaluate(Facts(target, group: paid), Today).BlockingReasons
            .Should().Contain(PlayerErasureBlockReasons.GroupPayments);
    }

    [Test]
    public void IsRetentionDue_ExactlyTwentyFourMonthsLater_IsDue()
    {
        PlayerErasureRules.IsRetentionDue(Today.AddMonths(-24), Today).Should().BeTrue();
        PlayerErasureRules.IsRetentionDue(Today.AddMonths(-24).AddDays(1), Today).Should().BeFalse();
        PlayerErasureRules.IsRetentionDue(null, Today).Should().BeFalse();
    }
}
