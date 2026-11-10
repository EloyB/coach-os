using CoachOS.Domain.Models;

namespace CoachOS.Application.WebsiteWaitlist;

public static class WebsiteWaitlistErrors
{
    public static readonly Error Closed = new(ErrorCodes.Gone, "De wachtlijst is gesloten.");
}
