using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.WebsiteContact;
using CoachOS.Application.WebsiteContact.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.Website;

public sealed class WebsiteContactEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        // Publiek formulier van de marketingsite: bewust anoniem, wel per IP begrensd.
        app.MapPost("/website/contact", async (SubmitWebsiteContactRequest request,
            IWebsiteContactService service, HttpContext context, CancellationToken ct) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            Result result = await service.SubmitAsync(request, ct);
            return result.IsSuccess ? Results.Ok(new { ok = true }) : result.ToErrorResult();
        })
        .AllowAnonymous()
        .RequireRateLimiting(WebsiteRateLimits.ContactPolicy)
        .AddEndpointFilter<ValidationFilter<SubmitWebsiteContactRequest>>()
        .WithTags("Website");
    }
}
