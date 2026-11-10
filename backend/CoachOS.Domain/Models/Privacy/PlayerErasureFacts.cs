using CoachOS.Domain.Enums;

namespace CoachOS.Domain.Models.Privacy;

/// <summary>De inschrijving die gewist zou worden.</summary>
public sealed record ErasureTarget(
    Guid Id,
    PrivacyErasureKind Kind,
    string Name,
    EnrollmentStatus Status,
    Guid? GroupId,
    DateTime UpdatedAt);

public sealed record ErasureGroupMember(Guid Id, string Name);

/// <summary>Groep (lessen of kamp) waar de doelinschrijving lid van is.</summary>
public sealed record ErasureGroupFacts(
    Guid GroupId,
    Guid LeaderEnrollmentId,
    IReadOnlyList<ErasureGroupMember> Members,
    bool HasPayments);

/// <summary>
/// Feiten over één inschrijving, binnen één organisatie, waarop de verwijderregels beslissen.
/// De repository verzamelt ze; de service beslist (zie <c>PlayerErasureRules</c>).
/// </summary>
public sealed record PlayerErasureFacts(
    ErasureTarget Target,
    IReadOnlyList<DateOnly> ParticipationDates,
    bool ParticipationChainIncomplete,
    bool HasPayments,
    bool HasOpenPaymentsForPlayer,
    ErasureGroupFacts? Group,
    bool GroupMissing,
    bool IsOrphanGroupLeader,
    bool HasPendingMail,
    bool HasUnresolvedRescheduleRequest,
    int FormAnswerCount);

public enum ErasureGroupEffect
{
    None = 0,
    LeaveGroup = 1,
    RemoveEmptyGroup = 2,
    TransferLeader = 3,
}

/// <summary>Wat de repository precies moet wissen.</summary>
public sealed record PlayerErasurePlan(
    PrivacyErasureKind Kind,
    Guid EnrollmentId,
    Guid? GroupId,
    ErasureGroupEffect GroupEffect,
    Guid? NewGroupLeaderId);

public enum ErasureLockOutcome
{
    Acquired = 0,
    NotFound = 1,
    LockTimeout = 2,
}

public enum ErasureDeleteOutcome
{
    Deleted = 0,
    DependencyConflict = 1,
}

public sealed record ErasureCandidate(Guid EnrollmentId, PrivacyErasureKind Kind);
