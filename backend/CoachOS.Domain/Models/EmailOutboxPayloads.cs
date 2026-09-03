namespace CoachOS.Domain.Models;

public sealed record EnrollmentConfirmationEmailPayload(
    string Email,
    string Name,
    string SeriesName,
    string TrainerName,
    IReadOnlyList<string> ParticipantNames);

public sealed record GroupMemberAddedEmailPayload(
    string Email,
    string Name,
    string SeriesName,
    string GroupName);

public sealed record TrainerEnrollmentNotificationEmailPayload(
    string TrainerEmail,
    string TrainerName,
    string StudentName,
    string StudentEmail,
    string SeriesName,
    IReadOnlyList<EmailResponseItem> Responses);

public sealed record EmailResponseItem(string FieldLabel, string Value);
