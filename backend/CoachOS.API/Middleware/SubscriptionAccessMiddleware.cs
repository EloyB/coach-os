using CoachOS.API.Auth;
using CoachOS.Application.Subscriptions;
using CoachOS.Application.Subscriptions.DTOs;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Models;

namespace CoachOS.API.Middleware;

/// <summary>
/// Dwingt enkel-lezen af voor organisaties waarvan de proef verlopen is: wijzigende requests
/// krijgen 402. Staat volledig uit zolang <see cref="BillingOptions.EnforceTrialExpiry"/> false
/// is — dan raakt deze middleware geen enkele request aan. Publieke (anonieme) flows zoals
/// inschrijfformulieren lopen altijd door.
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
        if (!IsGuardedRequest(context))
        {
            await next(context);
            return;
        }

        System.Security.Claims.Claim? orgClaim = context.User.FindFirst(CoachOsClaims.OrganizationId);
        if (orgClaim is null || !Guid.TryParse(orgClaim.Value, out Guid organizationId))
        {
            await next(context);
            return;
        }

        ISubscriptionAccessService accessService = context.RequestServices.GetRequiredService<ISubscriptionAccessService>();
        Result<SubscriptionStatusDto> status = await accessService.GetStatusAsync(organizationId, context.RequestAborted);
        if (status.IsSuccess && status.Value!.AccessLevel == SubscriptionAccessLevel.ReadOnly.ToString())
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

        string method = context.Request.Method;
        if (HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method))
            return false;

        string path = context.Request.Path.Value ?? string.Empty;
        return !AlwaysAllowedPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }
}
