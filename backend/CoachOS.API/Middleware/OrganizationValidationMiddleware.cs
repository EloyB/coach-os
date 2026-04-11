using CoachOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CoachOS.API.Middleware;

public class OrganizationValidationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ApplicationDbContext db, IMemoryCache cache)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        var orgClaim = context.User.FindFirst("organizationId");
        if (orgClaim is null || !Guid.TryParse(orgClaim.Value, out var orgId))
        {
            await next(context);
            return;
        }

        var cacheKey = $"org_exists_{orgId}";
        if (!cache.TryGetValue(cacheKey, out bool exists))
        {
            exists = await db.Organizations.AsNoTracking().AnyAsync(o => o.Id == orgId);
            cache.Set(cacheKey, exists, TimeSpan.FromMinutes(5));
        }

        if (!exists)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                message = "Organisatie bestaat niet meer. Log opnieuw in."
            });
            return;
        }

        await next(context);
    }
}
