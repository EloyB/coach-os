using System.Security.Claims;
using CoachOS.API.Auth;
using CoachOS.Infrastructure.Identity;
using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CoachOS.API.Middleware;

/// <summary>
/// Valideert per request dat de geauthenticeerde user nog actief is — en, voor super-admin
/// tokens, dat de IsSuperAdmin-flag nog steeds gezet is. JWT's zijn stateless, dus zonder
/// deze check blijven gedeactiveerde accounts tot token-expiry werken.
///
/// User-state wordt 30s gecached om DB-load te beperken. De korte TTL is een bewuste
/// trade-off: maximaal 30s vertraging op een disable, in ruil voor minder DB-hits.
/// </summary>
public class UserActiveValidationMiddleware(RequestDelegate next)
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    public async Task InvokeAsync(HttpContext context, ApplicationDbContext db, IMemoryCache cache)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        var subClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
        if (subClaim is null || !Guid.TryParse(subClaim.Value, out var userId))
        {
            await next(context);
            return;
        }

        var cacheKey = $"user_state_{userId}";
        if (!cache.TryGetValue(cacheKey, out UserState? state))
        {
            state = await db.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new UserState(u.IsActive, u.IsSuperAdmin))
                .FirstOrDefaultAsync();
            cache.Set(cacheKey, state, CacheTtl);
        }

        if (state is null || !state.IsActive)
        {
            await WriteUnauthorized(context, "Account is gedeactiveerd of bestaat niet meer.");
            return;
        }

        // Defense in depth: een super-admin token mag niet meer geldig zijn als de
        // user intussen z'n super-admin status verloren heeft.
        var tokenClaimsSuperAdmin = context.User.FindFirst(CoachOsClaims.IsSuperAdmin)?.Value == "true";
        if (tokenClaimsSuperAdmin && !state.IsSuperAdmin)
        {
            await WriteUnauthorized(context, "Super-admin rechten ingetrokken. Log opnieuw in.");
            return;
        }

        await next(context);
    }

    private static async Task WriteUnauthorized(HttpContext context, string message)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { message });
    }

    private sealed record UserState(bool IsActive, bool IsSuperAdmin);
}
