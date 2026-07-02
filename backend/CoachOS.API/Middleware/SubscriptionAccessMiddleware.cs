using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Subscriptions;

namespace CoachOS.API.Middleware;

/// <summary>
/// Blocks the app for organisations whose subscription grants no access
/// (trial expired / past-due grace elapsed / cancelled). Auth and billing
/// endpoints stay reachable so the user can pay and unlock. Data is never
/// touched — this only gates request handling.
/// </summary>
public class SubscriptionAccessMiddleware(RequestDelegate next)
{
    private static readonly string[] AllowPrefixes =
        ["/api/auth", "/api/billing", "/health"];

    public async Task InvokeAsync(
        HttpContext context,
        ISubscriptionRepository subscriptions,
        ITenantContext tenant)
    {
        string path = context.Request.Path.Value ?? string.Empty;

        bool allowlisted = AllowPrefixes.Any(p =>
            path.StartsWith(p, StringComparison.OrdinalIgnoreCase));

        if (allowlisted
            || context.User.Identity?.IsAuthenticated != true
            || tenant.OrganizationId == Guid.Empty)
        {
            await next(context);
            return;
        }

        var sub = await subscriptions.GetByOrganizationAsync(
            tenant.OrganizationId, context.RequestAborted);

        bool hasAccess = sub is not null && SubscriptionAccess.HasAppAccess(
            sub.Status, sub.TrialEndsAt, sub.CurrentPeriodEnd, DateTime.UtcNow);

        if (hasAccess)
        {
            await next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new { code = "subscription_required" });
    }
}
