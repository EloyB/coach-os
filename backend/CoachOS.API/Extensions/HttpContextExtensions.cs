using System.Security.Claims;

namespace CoachOS.API.Extensions;

public static class HttpContextExtensions
{
    public static Guid GetOrganizationId(this HttpContext context)
    {
        var claim = context.User.FindFirst("organizationId");
        if (claim is null || !Guid.TryParse(claim.Value, out var orgId))
            throw new UnauthorizedAccessException("Missing or invalid organizationId claim.");
        return orgId;
    }

    public static Guid GetUserId(this HttpContext context)
    {
        var claim = context.User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim is null || !Guid.TryParse(claim.Value, out var userId))
            throw new UnauthorizedAccessException("Missing or invalid user identifier claim.");
        return userId;
    }

    public static bool IsTrainer(this HttpContext context) =>
        context.User.IsInRole("Trainer");

    public static string GetEmail(this HttpContext context)
    {
        var claim = context.User.FindFirst(ClaimTypes.Email)
                    ?? context.User.FindFirst("email");
        if (claim is null || string.IsNullOrWhiteSpace(claim.Value))
            throw new UnauthorizedAccessException("Missing or invalid email claim.");
        return claim.Value;
    }
}
