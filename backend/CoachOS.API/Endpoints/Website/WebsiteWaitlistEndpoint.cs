using CoachOS.API.Filters;
using CoachOS.Application.WebsiteWaitlist;

namespace CoachOS.API.Endpoints.Website;

public sealed class WebsiteWaitlistEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/website/waitlist", async (RegisterWebsiteWaitlistRequest request,
            IWebsiteWaitlistService service, HttpContext context, CancellationToken ct) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            bool accepted = await service.RegisterAsync(request, ct);
            return accepted ? Results.Ok(new { ok = true }) : Results.StatusCode(410);
        })
        .AllowAnonymous()
        .RequireRateLimiting("website-waitlist")
        .AddEndpointFilter<ValidationFilter<RegisterWebsiteWaitlistRequest>>()
        .WithTags("Website");

        // Operator-only, one existing entry by ID, dry-run unless explicitly applied.
        // No public resend/token issuance and no arbitrary recipient/body/URL.
        app.MapPost("/super-admin/website-waitlist/{entryId:guid}/mail", async (Guid entryId,
            ManagedWebsiteWaitlistMailRequest request, IWebsiteWaitlistService service,
            HttpContext context, CancellationToken ct) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            return Results.Ok(await service.QueueManagedMailAsync(entryId, request.Apply, ct));
        })
        .RequireAuthorization(CoachOS.API.Auth.AuthorizationPolicies.SuperAdmin)
        .RequireRateLimiting("website-waitlist")
        .WithTags("Website");

        // No GET route: link scanners and prefetch cannot delete an entry.
        app.MapPost("/website/waitlist/unsubscribe", async (UnsubscribeWebsiteWaitlistRequest request,
            IWebsiteWaitlistService service, HttpContext context, CancellationToken ct) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            await service.UnsubscribeAsync(request, ct);
            return Results.Ok(new { ok = true });
        })
        .AllowAnonymous()
        .RequireRateLimiting("public")
        .AddEndpointFilter<ValidationFilter<UnsubscribeWebsiteWaitlistRequest>>()
        .WithTags("Website");
    }
}
