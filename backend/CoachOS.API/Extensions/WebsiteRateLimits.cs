using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace CoachOS.API.Extensions;

/// <summary>
/// Limieten voor de publieke websiteformulieren. Per bezoekers-IP (via ForwardedHeaders, enkel
/// vertrouwd vanaf het interne Docker-netwerk) zodat één bezoeker de quota van anderen niet kan
/// opgebruiken, plus een ruime globale noodrem per formulier tegen botnetten. IP's worden
/// enkel in het geheugen gebruikt, nooit opgeslagen.
/// </summary>
public static class WebsiteRateLimits
{
    public const string ContactPolicy = "website-contact";
    public const string WaitlistPolicy = "website-waitlist";
    public const string SuperAdminMailPolicy = "super-admin-website-mail";

    internal const int PerClientPermits = 5;
    internal static readonly TimeSpan PerClientWindow = TimeSpan.FromMinutes(10);
    internal const int SuperAdminPermits = 30;
    internal static readonly TimeSpan SuperAdminWindow = TimeSpan.FromHours(1);

    /// <summary>Globale noodrem per formulier, over alle IP's samen.</summary>
    internal static readonly IReadOnlyDictionary<string, int> BackstopPermitsPerHour = new Dictionary<string, int>
    {
        [ContactPolicy] = 100,
        [WaitlistPolicy] = 200,
    };

    public static void AddWebsitePolicies(this RateLimiterOptions options)
    {
        options.AddPolicy(ContactPolicy, context => PerClientPartition(context, ContactPolicy));
        options.AddPolicy(WaitlistPolicy, context => PerClientPartition(context, WaitlistPolicy));
        options.AddPolicy(SuperAdminMailPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
            $"{SuperAdminMailPolicy}:{ClientKey(context)}",
            _ => FixedWindow(SuperAdminPermits, SuperAdminWindow)));
    }

    internal static RateLimitPartition<string> PerClientPartition(HttpContext context, string policy)
        => RateLimitPartition.GetFixedWindowLimiter(
            $"{policy}:{ClientKey(context)}",
            _ => FixedWindow(PerClientPermits, PerClientWindow));

    /// <summary>
    /// Deel van de globale limiter: één gedeelde emmer per websiteformulier, geen limiet voor
    /// andere endpoints. Gebruikt de policynaam die het endpoint via RequireRateLimiting zet.
    /// </summary>
    internal static RateLimitPartition<string> BackstopPartition(HttpContext context)
    {
        string? policy = context.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName;
        if (policy is null || !BackstopPermitsPerHour.TryGetValue(policy, out int permits))
            return RateLimitPartition.GetNoLimiter("none");

        return RateLimitPartition.GetFixedWindowLimiter(
            $"backstop:{policy}",
            _ => FixedWindow(permits, TimeSpan.FromHours(1)));
    }

    private static string ClientKey(HttpContext context)
        => context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private static FixedWindowRateLimiterOptions FixedWindow(int permits, TimeSpan window) => new()
    {
        PermitLimit = permits,
        Window = window,
        QueueLimit = 0,
    };
}
