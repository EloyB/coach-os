using CoachOS.API.Auth;
using CoachOS.Application.Subscriptions;
using CoachOS.Application.Subscriptions.DTOs;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Models;

namespace CoachOS.API.Middleware;

/// <summary>
/// Dwingt enkel-lezen af voor organisaties waarvan de proef verlopen is: wijzigende requests
/// krijgen 402. Staat volledig uit zolang <see cref="BillingOptions.EnforceTrialExpiry"/> false
/// is, behalve de bestaande Locked-status. Publieke resources/capabilities bepalen
/// hun eigen tenant en kunnen de einddatum-/writegate niet anoniem omzeilen.
/// </summary>
public class SubscriptionAccessMiddleware(RequestDelegate next)
{
    // Paden die ook in enkel-lezen moeten blijven werken (sessiebeheer, eigen account).
    // /api/billing: een vergrendelde club moet altijd een plan kunnen kiezen en betalen.
    private static readonly string[] AlwaysAllowedPrefixes = ["/api/auth", "/api/subscription", "/api/super-admin", "/api/billing"];

    // Geen snelkoppeling op Billing:EnforceTrialExpiry: een vergrendeld account (Locked) is altijd
    // enkel-lezen. ISubscriptionAccessService past die instelling zelf toe op verlopen proeven.
    public async Task InvokeAsync(HttpContext context)
    {
        bool publicScope = context.Request.Path.StartsWithSegments("/api/public", StringComparison.OrdinalIgnoreCase);
        if (!publicScope && !IsGuardedRequest(context))
        {
            await next(context);
            return;
        }

        Guid organizationId = Guid.Empty;
        if (publicScope)
        {
            string[] segments = (context.Request.Path.Value ?? "").Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length >= 4)
            {
                string resource = segments[2].ToLowerInvariant(); // routing is case-insensitive; capability values are not
                string value = resource == "payments" && segments.Length >= 5 ? segments[4] : segments[3];
                IOrganizationPublicScopeResolver resolver = context.RequestServices.GetRequiredService<IOrganizationPublicScopeResolver>();
                organizationId = await resolver.ResolveAsync(resource, value, context.RequestAborted) ?? Guid.Empty;
            }
        }
        else
        {
            System.Security.Claims.Claim? orgClaim = context.User.FindFirst(CoachOsClaims.OrganizationId);
            if (orgClaim is not null) Guid.TryParse(orgClaim.Value, out organizationId);
        }
        if (organizationId == Guid.Empty)
        {
            await next(context);
            return;
        }

        ISubscriptionAccessService accessService = context.RequestServices.GetRequiredService<ISubscriptionAccessService>();
        Result<SubscriptionStatusDto> status = await accessService.GetStatusAsync(organizationId, context.RequestAborted);
        bool read = HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method) || HttpMethods.IsOptions(context.Request.Method);
        if (status.IsSuccess && (status.Value!.AccessLevel == SubscriptionAccessLevel.Closed.ToString() ||
            ((publicScope || !read || !context.User.IsInRole("Admin")) && status.Value!.AccessLevel == SubscriptionAccessLevel.ReadOnly.ToString())))
        {
            context.Response.StatusCode = StatusCodes.Status402PaymentRequired;
            context.Response.ContentType = "application/json";
            string message = status.Value.Status == SubscriptionStatus.Locked.ToString()
                ? "Je account is vergrendeld. Kies een abonnement om verder te werken."
                : "Je proefperiode is afgelopen. Je account staat op enkel-lezen.";
            await context.Response.WriteAsJsonAsync(new { message });
            return;
        }

        await next(context);
    }

    private static bool IsGuardedRequest(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return false;

        return !AlwaysAllowedPrefixes.Any(prefix => context.Request.Path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase));
    }
}
