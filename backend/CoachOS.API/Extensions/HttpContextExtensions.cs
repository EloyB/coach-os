using System.Security.Claims;
using CoachOS.API.Auth;

namespace CoachOS.API.Extensions;

public static class HttpContextExtensions
{
    public static Guid GetOrganizationId(this HttpContext context)
    {
        var claim = context.User.FindFirst(CoachOsClaims.OrganizationId);
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

    public static bool IsSuperAdmin(this HttpContext context) =>
        context.User.FindFirst(CoachOsClaims.IsSuperAdmin)?.Value == "true";

    public static bool IsAdmin(this HttpContext context) =>
        context.User.IsInRole("Admin");

    /// <summary>Club-id's waarvan de user hoofdtrainer is (0..n headTrainerClub-claims).</summary>
    public static IReadOnlyList<Guid> GetHeadTrainerClubIds(this HttpContext context) =>
        context.User.FindAll(CoachOsClaims.HeadTrainerClub)
            .Select(c => Guid.TryParse(c.Value, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .ToList();

    public static string GetEmail(this HttpContext context)
    {
        var claim = context.User.FindFirst(ClaimTypes.Email)
                    ?? context.User.FindFirst("email");
        if (claim is null || string.IsNullOrWhiteSpace(claim.Value))
            throw new UnauthorizedAccessException("Missing or invalid email claim.");
        return claim.Value;
    }
}
