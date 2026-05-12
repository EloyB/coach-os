using CoachOS.API.Extensions;
using CoachOS.Domain.Models;
using CoachOS.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace CoachOS.API.Endpoints.Dev;

/// <summary>
/// Dev-only seed endpoint dat een super-admin user aanmaakt zonder organisatie/membership.
/// Wordt enkel geregistreerd als <c>builder.Environment.IsDevelopment()</c> true is —
/// zie <see cref="EndpointMappingExtensions"/>. Bestaat zodat de seed-demo-data scripts
/// idempotent een super admin kunnen klaarzetten zonder direct in de DB te schrijven.
/// </summary>
public class BootstrapSuperAdminEndpoint : IEndpoint
{
    public record Request(string Email, string Password, string FirstName, string LastName);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/dev/super-admin/bootstrap", async (
            Request request,
            UserManager<ApplicationUser> userManager,
            CancellationToken ct) =>
        {
            var existing = await userManager.FindByEmailAsync(request.Email);
            if (existing is not null)
            {
                if (!existing.IsSuperAdmin)
                {
                    existing.IsSuperAdmin = true;
                    existing.IsActive = true;
                    existing.UpdatedAt = DateTime.UtcNow;
                    var update = await userManager.UpdateAsync(existing);
                    if (!update.Succeeded)
                        return Result.Fail(update.Errors.Select(e => e.Description)).ToErrorResult();
                }
                return Results.Ok(new { userId = existing.Id, created = false });
            }

            ApplicationUser user = new()
            {
                Id = Guid.NewGuid(),
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                IsActive = true,
                IsSuperAdmin = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            var create = await userManager.CreateAsync(user, request.Password);
            if (!create.Succeeded)
                return Result.Fail(create.Errors.Select(e => e.Description)).ToErrorResult();

            return Results.Ok(new { userId = user.Id, created = true });
        })
        .AllowAnonymous()
        .WithTags("Dev");
    }
}
