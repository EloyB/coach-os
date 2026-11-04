using CoachOS.API.Auth;
using CoachOS.Application.Subscriptions;

namespace CoachOS.API.Endpoints.SuperAdmin;

public class GetSubscriptionPlansEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/super-admin/subscription-plans", (ISubscriptionService service) =>
            Results.Ok(service.GetPlanCatalog()))
        .RequireAuthorization(AuthorizationPolicies.SuperAdmin)
        .WithTags("SuperAdmin");
    }
}
