namespace CoachOS.Application.Camps.DTOs;

public record CampEnrollmentResponseItemDto(string FieldLabel, string Value);

public record CampEnrollmentDto(
    Guid Id, string ParticipantName, string ParticipantEmail, string? ParticipantPhone,
    string Status, DateTime EnrolledAt, string? GroupName,
    List<CampEnrollmentResponseItemDto> FormResponses,
    string? PaymentMethod, string? PaymentStatus);
