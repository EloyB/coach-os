namespace CoachOS.Application.WebsiteWaitlist.DTOs;

public sealed record RegisterWebsiteWaitlistRequest(string Email, string? Role = null);

public sealed record UnsubscribeWebsiteWaitlistRequest(string Token);

public sealed record ManagedWebsiteWaitlistMailRequest(bool Apply = false);

public sealed record ManagedWebsiteWaitlistMailResult(bool Eligible, bool Queued);
