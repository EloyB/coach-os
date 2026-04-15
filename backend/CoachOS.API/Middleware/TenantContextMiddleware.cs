using System.Security.Claims;
using CoachOS.Infrastructure.MultiTenancy;

namespace CoachOS.API.Middleware;

/// <summary>
/// Leest <c>organizationId</c> en <c>sub</c> claims uit de JWT en vult de
/// scoped <see cref="TenantContext"/>. Moet ná <c>UseAuthentication</c> en
/// vóór de endpoints draaien. Anonieme requests passeren zonder te zetten.
/// </summary>
public class TenantContextMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, TenantContext tenant)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var orgClaim = context.User.FindFirst("organizationId");
            var userClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);

            if (orgClaim is not null
                && userClaim is not null
                && Guid.TryParse(orgClaim.Value, out var orgId)
                && Guid.TryParse(userClaim.Value, out var userId))
            {
                tenant.Set(orgId, userId);
            }
        }

        await next(context);
    }
}
