using Microsoft.AspNetCore.Builder;

namespace CoachOS.API.Extensions;

public static class AuthenticationRateLimitingExtensions
{
    public static IApplicationBuilder UseAuthenticatedRateLimiting(this IApplicationBuilder app)
    {
        // Account-partitioned policies need the validated principal, never unverified JWT data.
        app.UseAuthentication();
        app.UseRateLimiter();
        return app;
    }
}
