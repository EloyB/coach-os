using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.Trainers;
using CoachOS.Application.Trainers.DTOs;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.Trainers;

/// <summary>
/// Maakt een lid admin ("deputy admin") of zet het terug naar trainer. Enkel voor Admins.
/// De laatste actieve admin kan niet gedemoot worden.
/// </summary>
public class SetMemberRoleEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/trainers/{id:guid}/role",
            async (Guid id, SetMemberRoleRequest request, ITrainerService service, HttpContext ctx, CancellationToken ct) =>
            {
                UserRole role = request.Role == "Admin" ? UserRole.Admin : UserRole.Trainer;
                Result result = await service.SetRoleAsync(id, ctx.GetOrganizationId(), role, ct);
                return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
            })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .AddEndpointFilter<ValidationFilter<SetMemberRoleRequest>>()
        .WithTags("Trainers");
    }
}
