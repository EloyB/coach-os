using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.WebsiteWaitlist;
using CoachOS.Application.WebsiteWaitlist.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.Website;

public sealed class WebsiteWaitlistEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/website/waitlist", async (RegisterWebsiteWaitlistRequest request,
            IWebsiteWaitlistService service, HttpContext context, CancellationToken ct) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            Result result = await service.RegisterAsync(request, ct);
            return result.IsSuccess ? Results.Ok(new { ok = true }) : result.ToErrorResult();
        })
        .AllowAnonymous()
        .RequireRateLimiting(WebsiteRateLimits.WaitlistPolicy)
        .AddEndpointFilter<ValidationFilter<RegisterWebsiteWaitlistRequest>>()
        .WithTags("Website");

        // Enkel superadmin, een bestaande entry per ID, standaard dry-run.
        app.MapPost("/super-admin/website-waitlist/{entryId:guid}/mail", async (Guid entryId,
            ManagedWebsiteWaitlistMailRequest request, IWebsiteWaitlistService service,
            HttpContext context, CancellationToken ct) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            Result<ManagedWebsiteWaitlistMailResult> result = await service.QueueManagedMailAsync(entryId, request.Apply, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization(CoachOS.API.Auth.AuthorizationPolicies.SuperAdmin)
        .RequireRateLimiting(WebsiteRateLimits.SuperAdminMailPolicy)
        .WithTags("Website");

        // Geen GET-route: linkscanners en prefetch kunnen niets verwijderen.
        app.MapPost("/website/waitlist/unsubscribe", async (UnsubscribeWebsiteWaitlistRequest request,
            IWebsiteWaitlistService service, HttpContext context, CancellationToken ct) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            Result result = await service.UnsubscribeAsync(request, ct);
            return result.IsSuccess ? Results.Ok(new { ok = true }) : result.ToErrorResult();
        })
        .AllowAnonymous()
        .RequireRateLimiting(WebsiteRateLimits.UnsubscribePolicy)
        .AddEndpointFilter<ValidationFilter<UnsubscribeWebsiteWaitlistRequest>>()
        .WithTags("Website");
    }
}
