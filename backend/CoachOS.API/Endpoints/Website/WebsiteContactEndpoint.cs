using CoachOS.API.Filters;
using CoachOS.Application.WebsiteContact;

namespace CoachOS.API.Endpoints.Website;

public sealed class WebsiteContactEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/website/contact", async (SubmitWebsiteContactRequest request, IWebsiteContactService service, HttpContext context, CancellationToken ct) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            await service.SubmitAsync(request, ct);
            return Results.Ok(new { ok = true });
        }).AllowAnonymous().RequireRateLimiting("website-contact")
            .AddEndpointFilter<ValidationFilter<SubmitWebsiteContactRequest>>().WithTags("Website");
    }
}
