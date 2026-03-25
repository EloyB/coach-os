using System.Security.Claims;

namespace CoachOS.API.Extensions;

public static class HttpContextExtensions
{
    public static Guid GetOrganizationId(this HttpContext context) =>
        Guid.Parse(context.User.FindFirst("organizationId")!.Value);

    public static Guid GetUserId(this HttpContext context) =>
        Guid.Parse(context.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    public static bool IsTrainer(this HttpContext context) =>
        context.User.IsInRole("Trainer");
}
