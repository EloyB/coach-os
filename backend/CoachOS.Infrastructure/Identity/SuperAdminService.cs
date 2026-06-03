using System.Security.Cryptography;
using CoachOS.Application.SuperAdmin;
using CoachOS.Application.SuperAdmin.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using CoachOS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoachOS.Infrastructure.Identity;

public class SuperAdminService(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext context,
    IEmailService emailService,
    ILogger<SuperAdminService> logger)
    : ISuperAdminService
{
    public async Task<Result<Guid>> CreateAdminWithOrganizationAsync(
        CreateAdminRequest request,
        string inviteBaseUrl,
        CancellationToken ct = default)
    {
        // Voorkom dubbele accounts: één e-mailadres = één user. Als hij al admin/trainer
        // is van een andere org dan moet de super admin manueel beslissen — niet automatisch
        // merging — want anders kan een quirky permissie-state ontstaan.
        var existing = await userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
        {
            logger.LogWarning(
                "SuperAdmin CreateAdmin: e-mail {Email} bestaat al (UserId: {UserId}) — geweigerd",
                MaskEmail(request.Email), existing.Id);
            return Result<Guid>.Fail("E-mailadres is al in gebruik");
        }

        await using var tx = await context.Database.BeginTransactionAsync(ct);
        try
        {
            Organization organization = new()
            {
                Id = Guid.NewGuid(),
                Name = request.OrganizationName.Trim(),
                Email = request.Email.Trim(),
                Country = "BE",
                IsActive = true,
                IsEarlyBird = request.IsEarlyBird
            };
            context.Organizations.Add(organization);

            // Initialiseer org-settings met defaults — zelfde patroon als AuthService.RegisterAsync.
            // Voorkomt dat een GET /organization/settings vanuit de FE eerst een lazy
            // provisioning trip moet doen, en houdt het bestaan van settings expliciet
            // aan de creatie-transactie gekoppeld.
            // OnboardingStartedAt stempelen we ook hier: een door super-admin aangemaakte org
            // is even "nieuw" als een self-registered org en moet dezelfde onboarding zien.
            context.OrganizationSettings.Add(new Domain.Entities.OrganizationSettings
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                AdminsActAsTrainers = true,
                OnboardingStartedAt = DateTime.UtcNow,
            });

            await context.SaveChangesAsync(ct);

            var inviteToken = Guid.NewGuid().ToString("N");
            var hashedToken = HashToken(inviteToken);

            ApplicationUser user = new()
            {
                Id = Guid.NewGuid(),
                UserName = request.Email.Trim(),
                Email = request.Email.Trim(),
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                IsActive = false,
                InviteToken = hashedToken,
                InviteTokenExpiry = DateTime.UtcNow.AddHours(72),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Een random, sterk wachtwoord — wordt overschreven door AcceptInviteAsync.
            // Identity vereist een wachtwoord bij CreateAsync.
            var bootstrapPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) + "!A1";
            var createResult = await userManager.CreateAsync(user, bootstrapPassword);
            if (!createResult.Succeeded)
            {
                await tx.RollbackAsync(ct);
                return Result<Guid>.Fail(createResult.Errors.Select(e => e.Description));
            }

            context.OrganizationMemberships.Add(new OrganizationMembership
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                OrganizationId = organization.Id,
                Role = UserRole.Admin,
                IsActive = false, // wordt actief bij AcceptInvite
                JoinedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync(ct);

            await tx.CommitAsync(ct);

            var inviteUrl = $"{inviteBaseUrl.TrimEnd('/')}/invite/{inviteToken}";
            await emailService.SendAdminInviteAsync(user.Email!, user.FirstName, organization.Name, inviteUrl, ct);

            logger.LogInformation(
                "SuperAdmin heeft admin aangemaakt: {Email} (UserId: {UserId}) voor organisatie {OrgName} ({OrgId}), early-bird={EarlyBird}",
                MaskEmail(user.Email!), user.Id, organization.Name, organization.Id, organization.IsEarlyBird);

            return Result<Guid>.Ok(user.Id);
        }
        catch (DbUpdateException ex)
        {
            await tx.RollbackAsync(ct);
            logger.LogError(ex, "SuperAdmin CreateAdmin mislukt voor {Email}", MaskEmail(request.Email));
            return Result<Guid>.Fail("Aanmaak mislukt. Probeer het opnieuw.");
        }
    }

    public async Task<Result<List<AdminListItemDto>>> ListAdminsAsync(CancellationToken ct = default)
    {
        // Een admin = elke user die minstens één Admin-membership heeft (actief of pending).
        // Cross-org: we filteren bewust NIET op OrganizationId.
        var adminUserIds = await context.OrganizationMemberships
            .AsNoTracking()
            .Where(m => m.Role == UserRole.Admin)
            .Select(m => m.UserId)
            .Distinct()
            .ToListAsync(ct);

        if (adminUserIds.Count == 0)
            return Result<List<AdminListItemDto>>.Ok([]);

        var users = await context.Users
            .AsNoTracking()
            .Where(u => adminUserIds.Contains(u.Id) && !u.IsSuperAdmin)
            .OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
            .ToListAsync(ct);

        var memberships = await context.OrganizationMemberships
            .AsNoTracking()
            .Include(m => m.Organization)
            .Where(m => adminUserIds.Contains(m.UserId) && m.Role == UserRole.Admin)
            .ToListAsync(ct);

        var result = users.Select(u => new AdminListItemDto
        {
            UserId = u.Id,
            FirstName = u.FirstName,
            LastName = u.LastName,
            Email = u.Email ?? string.Empty,
            IsActive = u.IsActive,
            InvitePending = u.InviteToken is not null,
            CreatedAt = u.CreatedAt,
            Organizations = memberships
                .Where(m => m.UserId == u.Id)
                .Select(m => new AdminOrganizationDto
                {
                    OrganizationId = m.OrganizationId,
                    OrganizationName = m.Organization?.Name ?? string.Empty,
                    IsEarlyBird = m.Organization?.IsEarlyBird ?? false,
                    MembershipActive = m.IsActive
                })
                .ToList()
        }).ToList();

        return Result<List<AdminListItemDto>>.Ok(result);
    }

    public async Task<Result> DisableAdminAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Result.Fail(new Error(ErrorCodes.NotFound, "User niet gevonden."));

        if (user.IsSuperAdmin)
            return Result.Fail(new Error(ErrorCodes.Validation, "Een super admin kan niet via deze actie gedeactiveerd worden."));

        if (!user.IsActive)
            return Result.Ok();

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return Result.Fail(result.Errors.Select(e => e.Description));

        logger.LogWarning("SuperAdmin heeft admin gedeactiveerd: {Email} (UserId: {UserId})", MaskEmail(user.Email!), user.Id);
        return Result.Ok();
    }

    public async Task<Result> EnableAdminAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Result.Fail(new Error(ErrorCodes.NotFound, "User niet gevonden."));

        if (user.IsSuperAdmin)
            return Result.Fail(new Error(ErrorCodes.Validation, "Een super admin kan niet via deze actie geactiveerd worden."));

        if (user.IsActive)
            return Result.Ok();

        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return Result.Fail(result.Errors.Select(e => e.Description));

        logger.LogInformation("SuperAdmin heeft admin geheractiveerd: {Email} (UserId: {UserId})", MaskEmail(user.Email!), user.Id);
        return Result.Ok();
    }

    public async Task<Result> ResendAdminInviteAsync(Guid userId, string inviteBaseUrl, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Result.Fail(new Error(ErrorCodes.NotFound, "User niet gevonden."));

        if (user.IsActive || user.InviteToken is null)
            return Result.Fail(new Error(ErrorCodes.Validation, "Deze admin heeft al een wachtwoord ingesteld."));

        // Rate limit: niet vaker dan elke 5 minuten een nieuwe mail.
        if (user.InviteTokenExpiry.HasValue)
        {
            var issuedAt = user.InviteTokenExpiry.Value.AddHours(-72);
            if (DateTime.UtcNow - issuedAt < TimeSpan.FromMinutes(5))
                return Result.Fail(new Error(ErrorCodes.Validation, "Wacht minstens 5 minuten voordat je opnieuw een uitnodiging verstuurt."));
        }

        // Vind de bijbehorende admin-membership (cross-org — een super admin kan elke admin opnieuw uitnodigen).
        var membership = await context.OrganizationMemberships
            .AsNoTracking()
            .Include(m => m.Organization)
            .FirstOrDefaultAsync(m => m.UserId == userId && m.Role == UserRole.Admin, ct);

        if (membership is null)
            return Result.Fail(new Error(ErrorCodes.NotFound, "Geen admin-membership gevonden."));

        var newToken = Guid.NewGuid().ToString("N");
        user.InviteToken = HashToken(newToken);
        user.InviteTokenExpiry = DateTime.UtcNow.AddHours(72);
        user.UpdatedAt = DateTime.UtcNow;

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return Result.Fail(updateResult.Errors.Select(e => e.Description));

        var inviteUrl = $"{inviteBaseUrl.TrimEnd('/')}/invite/{newToken}";
        await emailService.SendAdminInviteAsync(
            user.Email!, user.FirstName, membership.Organization?.Name ?? "CoachOS", inviteUrl, ct);

        logger.LogInformation("SuperAdmin heeft invite opnieuw verstuurd: {Email} (UserId: {UserId})", MaskEmail(user.Email!), user.Id);
        return Result.Ok();
    }

    public async Task<Result<List<OrganizationListItemDto>>> ListOrganizationsAsync(CancellationToken ct = default)
    {
        var orgs = await context.Organizations
            .AsNoTracking()
            .OrderBy(o => o.Name)
            .Select(o => new OrganizationListItemDto
            {
                Id = o.Id,
                Name = o.Name,
                Email = o.Email,
                IsActive = o.IsActive,
                IsEarlyBird = o.IsEarlyBird,
                CreatedAt = o.CreatedAt,
                AdminCount = context.OrganizationMemberships.Count(m =>
                    m.OrganizationId == o.Id && m.Role == UserRole.Admin && m.IsActive)
            })
            .ToListAsync(ct);

        return Result<List<OrganizationListItemDto>>.Ok(orgs);
    }

    public async Task<Result> SetOrganizationEarlyBirdAsync(Guid organizationId, bool isEarlyBird, CancellationToken ct = default)
    {
        var org = await context.Organizations.FirstOrDefaultAsync(o => o.Id == organizationId, ct);
        if (org is null)
            return Result.Fail(new Error(ErrorCodes.NotFound, "Organisatie niet gevonden."));

        if (org.IsEarlyBird == isEarlyBird)
            return Result.Ok();

        org.IsEarlyBird = isEarlyBird;
        org.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "SuperAdmin heeft early-bird flag gezet op organisatie {OrgName} ({OrgId}) → {Value}",
            org.Name, org.Id, isEarlyBird);
        return Result.Ok();
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToHexStringLower(bytes);
    }

    private static string MaskEmail(string email)
    {
        if (string.IsNullOrEmpty(email)) return email;
        var at = email.IndexOf('@');
        if (at <= 0) return "***";
        var local = email[..at];
        var domain = email[(at + 1)..];
        var visible = local.Length <= 2 ? local[..1] : local[..2];
        return $"{visible}***@{domain}";
    }
}
