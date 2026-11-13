using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace CoachOS.API.Extensions;

/// <summary>
/// Eigen limiet voor de Mollie-webhooks (Connect en billing), los van de gedeelde <c>public</c>-limiet
/// van de inschrijfformulieren: een bot op die formulieren mag webhooks niet laten falen, en een
/// piek aan webhooks mag de formulieren niet blokkeren. Eén gedeelde emmer, want Mollie verstuurt
/// vanaf meerdere IP's. Ruim genoeg voor pieken (bv. veel verlengingen op dezelfde dag); de
/// webhook haalt de status altijd zelf op bij Mollie, dus misbruik levert niets op.
/// </summary>
public static class WebhookRateLimits
{
    public const string MolliePolicy = "mollie-webhook";

    internal const int MolliePermitsPerMinute = 300;

    public static void AddWebhookPolicies(this RateLimiterOptions options)
        => options.AddPolicy(MolliePolicy, MolliePartition);

    internal static RateLimitPartition<string> MolliePartition(HttpContext context)
        => RateLimitPartition.GetFixedWindowLimiter(MolliePolicy, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = MolliePermitsPerMinute,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        });
}
