namespace CoachOS.Application.Camps.DTOs;

public record CampEnrollmentResponseItemDto(Guid FieldId, string FieldLabel, string Value);

/// <param name="MolliePaymentId">Mollie-referentie (tr_...) om de betaling in het Mollie-dashboard terug te vinden.</param>
public record CampEnrollmentDto(
    Guid Id, string ParticipantName, string ParticipantEmail, string? ParticipantPhone,
    string Status, DateTime EnrolledAt, string? GroupName,
    Guid? EnrollmentGroupId, bool IsGroupLeader,
    List<CampEnrollmentResponseItemDto> FormResponses,
    string? PaymentMethod, string? PaymentStatus,
    decimal? PaymentAmount, DateTime? PaidAt, string? MolliePaymentId);
