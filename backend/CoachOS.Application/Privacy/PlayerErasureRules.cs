using CoachOS.Domain.Enums;
using CoachOS.Domain.Models.Privacy;

namespace CoachOS.Application.Privacy;

/// <summary>Stabiele codes voor wat een verwijdering blokkeert (vertaald in de frontend).</summary>
public static class PlayerErasureBlockReasons
{
    public const string ActiveOrFutureParticipation = "active_or_future_participation";
    public const string ParticipationUnknown = "participation_unknown";
    public const string ParticipationChainIncomplete = "participation_chain_incomplete";
    public const string FinancialRecords = "financial_records_present";
    public const string OpenPayment = "open_payment";
    public const string GroupPayments = "group_payments_present";
    public const string GroupInconsistent = "group_inconsistent";
    public const string PendingMail = "pending_mail";
    public const string UnresolvedRescheduleRequest = "unresolved_reschedule_request";
}

/// <summary>Wat de automatische bewaartermijn met één inschrijving doet.</summary>
public enum RetentionDecision
{
    /// <summary>Termijn nog niet verstreken (of deelname onbekend).</summary>
    NotDue = 0,
    Erase = 1,

    /// <summary>Enkel betalingen blokkeren: persoonsgegevens wissen, betalingen bewaren.</summary>
    Anonymize = 2,

    /// <summary>Termijn verstreken, maar een andere blokkering of een groepskeuze houdt het tegen.</summary>
    Blocked = 3,
}

public sealed record PlayerErasureEvaluation(
    DateOnly? LastParticipation,
    IReadOnlyList<string> BlockingReasons,
    ErasureGroupEffect GroupEffect,
    IReadOnlyList<ErasureGroupMember> OtherGroupMembers)
{
    public bool CanErase => BlockingReasons.Count == 0;
}

/// <summary>
/// Pure beslisregels. Betalingen (open of voltooid) blokkeren altijd: financiële records moeten
/// bewaard blijven. Een geprijsd aanbod zonder enige betaalrij blokkeert níet.
/// </summary>
public static class PlayerErasureRules
{
    public const int RetentionMonths = 24;

    public static bool IsRetentionDue(DateOnly? lastParticipation, DateOnly today) =>
        lastParticipation is not null && lastParticipation.Value.AddMonths(RetentionMonths) <= today;

    /// <summary>
    /// Beslissing van de automatische bewaartermijn. Strenger dan de handmatige verwijdering:
    /// een groepsleider wordt nooit automatisch vervangen. Anonimiseren gebeurt enkel als
    /// voltooide betalingen van de speler zelf de énige blokkering zijn en hij in geen groep zit;
    /// open betalingen en groepsbetalingen blijven blokkeren.
    /// </summary>
    public static RetentionDecision DecideRetention(PlayerErasureFacts facts, PlayerErasureEvaluation evaluation, DateOnly today)
    {
        if (!IsRetentionDue(evaluation.LastParticipation, today)) return RetentionDecision.NotDue;
        if (evaluation.GroupEffect == ErasureGroupEffect.TransferLeader) return RetentionDecision.Blocked;
        if (evaluation.CanErase) return RetentionDecision.Erase;
        bool onlyOwnPayments = evaluation.BlockingReasons.All(r => r == PlayerErasureBlockReasons.FinancialRecords);
        return onlyOwnPayments && facts.Group is null ? RetentionDecision.Anonymize : RetentionDecision.Blocked;
    }

    public static PlayerErasureEvaluation Evaluate(PlayerErasureFacts facts, DateOnly today)
    {
        DateOnly? last = facts.ParticipationDates.Count == 0 ? null : facts.ParticipationDates.Max();
        List<string> reasons = [];
        AddParticipationReasons(facts, last, today, reasons);
        AddFinancialReasons(facts, reasons);
        if (IsGroupInconsistent(facts)) reasons.Add(PlayerErasureBlockReasons.GroupInconsistent);
        if (facts.HasPendingMail) reasons.Add(PlayerErasureBlockReasons.PendingMail);
        if (facts.HasUnresolvedRescheduleRequest) reasons.Add(PlayerErasureBlockReasons.UnresolvedRescheduleRequest);

        IReadOnlyList<ErasureGroupMember> others = facts.Group?.Members
            .Where(m => m.Id != facts.Target.Id).ToList() ?? [];
        return new PlayerErasureEvaluation(last, reasons, GroupEffectFor(facts), others);
    }

    private static void AddParticipationReasons(
        PlayerErasureFacts facts, DateOnly? last, DateOnly today, List<string> reasons)
    {
        if (facts.ParticipationChainIncomplete) reasons.Add(PlayerErasureBlockReasons.ParticipationChainIncomplete);
        if (last is null) reasons.Add(PlayerErasureBlockReasons.ParticipationUnknown);
        else if (last.Value >= today) reasons.Add(PlayerErasureBlockReasons.ActiveOrFutureParticipation);
    }

    private static void AddFinancialReasons(PlayerErasureFacts facts, List<string> reasons)
    {
        if (facts.HasPayments) reasons.Add(PlayerErasureBlockReasons.FinancialRecords);
        if (facts.Target.Status == EnrollmentStatus.PendingPayment || facts.HasOpenPaymentsForPlayer)
            reasons.Add(PlayerErasureBlockReasons.OpenPayment);
        if (facts.Group?.HasPayments == true) reasons.Add(PlayerErasureBlockReasons.GroupPayments);
    }

    private static bool IsGroupInconsistent(PlayerErasureFacts facts)
    {
        if (facts.GroupMissing || facts.IsOrphanGroupLeader) return true;
        if (facts.Group is null) return false;
        return facts.Group.Members.All(m => m.Id != facts.Group.LeaderEnrollmentId)
            || facts.Group.Members.All(m => m.Id != facts.Target.Id);
    }

    private static ErasureGroupEffect GroupEffectFor(PlayerErasureFacts facts)
    {
        if (facts.Group is null) return ErasureGroupEffect.None;
        if (facts.Group.LeaderEnrollmentId != facts.Target.Id) return ErasureGroupEffect.LeaveGroup;
        return facts.Group.Members.Count > 1 ? ErasureGroupEffect.TransferLeader : ErasureGroupEffect.RemoveEmptyGroup;
    }
}
