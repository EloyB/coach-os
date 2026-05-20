namespace CoachOS.Application.MollieConnect.DTOs;

/// <summary>
/// Response van <c>POST /api/mollie-connect/start</c>. Frontend doet vervolgens
/// <c>window.location.href = AuthorizationUrl</c>.
/// </summary>
public record StartConnectResponse(string AuthorizationUrl);
