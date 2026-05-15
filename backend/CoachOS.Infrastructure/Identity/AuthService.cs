using CoachOS.Application.Auth;
using CoachOS.Application.Auth.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using CoachOS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoachOS.Infrastructure.Identity;

public class AuthService(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext context,
    TokenService tokenService,
    IEmailService emailService,
    ILogger<AuthService> logger)
    : IAuthService
{
    public async Task<Result<AuthResponseDto>> RegisterAsync(
        string organizationName,
        string firstName,
        string lastName,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        // Bij registratie van een nieuwe organisatie mag dezelfde email niet
        // opnieuw een nieuwe org aanmaken — dan groeien de tenants ongecontroleerd.
        // Wil je als bestaande user een nieuwe org starten, dan gebeurt dat via
        // een aparte "create organization" flow (post-MVP).
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            logger.LogWarning("Registratie geweigerd: e-mail {Email} is al in gebruik", MaskEmail(email));
            return Result<AuthResponseDto>.Fail("E-mailadres is al in gebruik");
        }

        await using var transaction =
            await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            Organization organization = new()
            {
                Id = Guid.NewGuid(),
                Name = organizationName,
                Email = email,
                IsActive = true,
                Country = "BE"
            };

            context.Organizations.Add(organization);

            // Initialiseer org-settings met defaults zodat de FE bij eerste GET geen lazy
            // provisioning hoeft te triggeren. AdminsActAsTrainers default true: een
            // tennisschool draait typisch op de admin-coach combinatie.
            context.OrganizationSettings.Add(new Domain.Entities.OrganizationSettings
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                AdminsActAsTrainers = true,
            });

            await context.SaveChangesAsync(cancellationToken);

            ApplicationUser user = new()
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                IsActive = true
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result<AuthResponseDto>.Fail(result.Errors.Select(e => e.Description));
            }

            OrganizationMembership membership = new()
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                OrganizationId = organization.Id,
                Role = UserRole.Admin,
                IsActive = true,
                JoinedAt = DateTime.UtcNow
            };
            context.OrganizationMemberships.Add(membership);
            await context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation("Nieuwe registratie: {Email}, organisatie {OrgName} ({OrgId})",
                email, organizationName, organization.Id);

            (var token, var expiresAt) = tokenService.GenerateToken(user, membership);

            return Result<AuthResponseDto>.Ok(new AuthResponseDto
            {
                Token = token,
                ExpiresAt = expiresAt,
                UserId = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                OrganizationId = organization.Id,
                Role = UserRole.Admin.ToString(),
                Memberships =
                [
                    new OrganizationMembershipDto
                    {
                        OrganizationId = organization.Id,
                        OrganizationName = organization.Name,
                        Role = UserRole.Admin.ToString(),
                        IsActive = true
                    }
                ]
            });
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Registratie mislukt voor {Email}", email);
            return Result<AuthResponseDto>.Fail("Registratie mislukt. Probeer het opnieuw.");
        }
    }

    public async Task<Result<AuthResponseDto>> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            logger.LogWarning("Mislukte inlogpoging: onbekend e-mailadres {Email}", MaskEmail(email));
            return Result<AuthResponseDto>.Fail("Ongeldige inloggegevens");
        }

        var validPassword = await userManager.CheckPasswordAsync(user, password);
        if (!validPassword)
        {
            logger.LogWarning("Mislukte inlogpoging: fout wachtwoord voor {Email} (UserId: {UserId})", MaskEmail(email), user.Id);
            return Result<AuthResponseDto>.Fail("Ongeldige inloggegevens");
        }

        if (!user.IsActive)
        {
            logger.LogWarning("Inlogpoging op gedeactiveerd account: {Email} (UserId: {UserId})", MaskEmail(email), user.Id);
            return Result<AuthResponseDto>.Fail("Account is gedeactiveerd");
        }

        var memberships = await LoadActiveMembershipsAsync(user.Id, cancellationToken);
        if (memberships.Count == 0)
        {
            logger.LogWarning("Login zonder actieve memberships: {Email} (UserId: {UserId})", MaskEmail(email), user.Id);
            return Result<AuthResponseDto>.Fail("Geen actieve organisatie gekoppeld aan dit account");
        }

        // Standaard: eerste membership. Bij Admin-rol krijgt die voorrang zodat
        // admins niet elke keer expliciet moeten switchen.
        var active = memberships.FirstOrDefault(m => m.Role == UserRole.Admin) ?? memberships[0];

        logger.LogInformation("Succesvolle login: {Email} (UserId: {UserId}), actieve org {OrgId}",
            email, user.Id, active.OrganizationId);

        return BuildAuthResponse(user, active, memberships);
    }

    public async Task<Result<AuthResponseDto>> SwitchOrganizationAsync(
        Guid userId,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive)
            return Result<AuthResponseDto>.Fail("Account niet gevonden");

        var memberships = await LoadActiveMembershipsAsync(userId, cancellationToken);
        var target = memberships.FirstOrDefault(m => m.OrganizationId == organizationId);
        if (target is null)
            return Result<AuthResponseDto>.Fail("Geen toegang tot deze organisatie");

        return BuildAuthResponse(user, target, memberships);
    }

    public async Task<Result<List<OrganizationMembershipDto>>> GetMembershipsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var memberships = await LoadActiveMembershipsAsync(userId, cancellationToken);
        return Result<List<OrganizationMembershipDto>>.Ok(memberships.Select(ToDto).ToList());
    }

    public async Task<Result> ForgotPasswordAsync(
        string email,
        string resetBaseUrl,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email);

        // Anti-enumeration: always return Ok, even when e-mail not found.
        if (user is null || !user.IsActive)
        {
            logger.LogWarning("Wachtwoord-reset aangevraagd voor onbekend/inactief e-mail {Email}", MaskEmail(email));
            return Result.Ok();
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = Uri.EscapeDataString(token);
        var encodedEmail = Uri.EscapeDataString(email);
        var resetUrl = $"{resetBaseUrl.TrimEnd('/')}/reset-password?token={encodedToken}&email={encodedEmail}";

        await emailService.SendPasswordResetAsync(email, user.FirstName, resetUrl, cancellationToken);

        logger.LogInformation("Wachtwoord-reset e-mail verstuurd naar {Email}", MaskEmail(email));
        return Result.Ok();
    }

    public async Task<Result> ResetPasswordAsync(
        string email,
        string token,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null || !user.IsActive)
            return Result.Fail(new Error(ErrorCodes.Validation, "Ongeldige reset-link"));

        var result = await userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
        {
            logger.LogWarning("Wachtwoord-reset mislukt voor {Email}: {Errors}",
                MaskEmail(email), string.Join(", ", result.Errors.Select(e => e.Description)));
            return Result.Fail(result.Errors.Select(e => e.Description));
        }

        logger.LogInformation("Wachtwoord succesvol gereset voor {Email}", MaskEmail(email));
        return Result.Ok();
    }

    private async Task<List<OrganizationMembership>> LoadActiveMembershipsAsync(Guid userId, CancellationToken ct)
    {
        return await context.OrganizationMemberships
            .AsNoTracking()
            .Include(m => m.Organization)
            .Where(m => m.UserId == userId && m.IsActive)
            .OrderBy(m => m.Organization.Name)
            .ToListAsync(ct);
    }

    private Result<AuthResponseDto> BuildAuthResponse(
        ApplicationUser user,
        OrganizationMembership active,
        IReadOnlyList<OrganizationMembership> all)
    {
        (var token, var expiresAt) = tokenService.GenerateToken(user, active);
        return Result<AuthResponseDto>.Ok(new AuthResponseDto
        {
            Token = token,
            ExpiresAt = expiresAt,
            UserId = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            OrganizationId = active.OrganizationId,
            Role = active.Role.ToString(),
            Memberships = all.Select(ToDto).ToList()
        });
    }

    private static OrganizationMembershipDto ToDto(OrganizationMembership m) => new()
    {
        OrganizationId = m.OrganizationId,
        OrganizationName = m.Organization?.Name ?? string.Empty,
        Role = m.Role.ToString(),
        IsActive = m.IsActive
    };

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
