namespace CoachOS.Application.WebsiteContact.DTOs;

public sealed record SubmitWebsiteContactRequest(string Name, string Email, string Message, string? Organization = null);
