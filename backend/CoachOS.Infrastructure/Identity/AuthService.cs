using CoachOS.Application.Auth;
using CoachOS.Application.Auth.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
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
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            logger.LogWarning("Registratie geweigerd: e-mail {Email} is al in gebruik", email);
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
            await context.SaveChangesAsync(cancellationToken);

            ApplicationUser user = new()
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                OrganizationId = organization.Id,
                Role = UserRole.Admin,
                IsActive = true
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result<AuthResponseDto>.Fail(result.Errors.Select(e => e.Description));
            }

            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation("Nieuwe registratie: {Email}, organisatie {OrgName} ({OrgId})",
                email, organizationName, organization.Id);

            (var token, var expiresAt) = tokenService.GenerateToken(user);

            return Result<AuthResponseDto>.Ok(new AuthResponseDto
            {
                Token = token,
                ExpiresAt = expiresAt,
                UserId = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                OrganizationId = user.OrganizationId,
                Role = user.Role.ToString()
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
            logger.LogWarning("Mislukte inlogpoging: onbekend e-mailadres {Email}", email);
            return Result<AuthResponseDto>.Fail("Ongeldige inloggegevens");
        }

        var validPassword = await userManager.CheckPasswordAsync(user, password);
        if (!validPassword)
        {
            logger.LogWarning("Mislukte inlogpoging: fout wachtwoord voor {Email} (UserId: {UserId})", email, user.Id);
            return Result<AuthResponseDto>.Fail("Ongeldige inloggegevens");
        }

        if (!user.IsActive)
        {
            logger.LogWarning("Inlogpoging op gedeactiveerd account: {Email} (UserId: {UserId})", email, user.Id);
            return Result<AuthResponseDto>.Fail("Account is gedeactiveerd");
        }

        logger.LogInformation("Succesvolle login: {Email} (UserId: {UserId})", email, user.Id);

        (var token, var expiresAt) = tokenService.GenerateToken(user);

        return Result<AuthResponseDto>.Ok(new AuthResponseDto
        {
            Token = token,
            ExpiresAt = expiresAt,
            UserId = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            OrganizationId = user.OrganizationId,
            Role = user.Role.ToString()
        });
    }
}
