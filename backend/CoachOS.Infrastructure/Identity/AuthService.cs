using CoachOS.Application.Auth;
using CoachOS.Application.Auth.DTOs;
using CoachOS.Application.Common.Models;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Identity;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly TokenService _tokenService;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        TokenService tokenService)
    {
        _userManager = userManager;
        _context = context;
        _tokenService = tokenService;
    }

    public async Task<Result<AuthResponseDto>> RegisterAsync(
        string organizationName,
        string firstName,
        string lastName,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        ApplicationUser? existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser is not null)
            return Result<AuthResponseDto>.Failure("E-mailadres is al in gebruik");

        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction =
            await _context.Database.BeginTransactionAsync(cancellationToken);

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

            _context.Organizations.Add(organization);
            await _context.SaveChangesAsync(cancellationToken);

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

            IdentityResult result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result<AuthResponseDto>.Failure(result.Errors.Select(e => e.Description));
            }

            await transaction.CommitAsync(cancellationToken);

            (string token, DateTime expiresAt) = _tokenService.GenerateToken(user);

            return Result<AuthResponseDto>.Success(new AuthResponseDto
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
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<AuthResponseDto>.Failure("Registratie mislukt. Probeer het opnieuw.");
        }
    }

    public async Task<Result<AuthResponseDto>> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        ApplicationUser? user = await _userManager.FindByEmailAsync(email);
        if (user is null)
            return Result<AuthResponseDto>.Failure("Ongeldige inloggegevens");

        bool validPassword = await _userManager.CheckPasswordAsync(user, password);
        if (!validPassword)
            return Result<AuthResponseDto>.Failure("Ongeldige inloggegevens");

        if (!user.IsActive)
            return Result<AuthResponseDto>.Failure("Account is gedeactiveerd");

        (string token, DateTime expiresAt) = _tokenService.GenerateToken(user);

        return Result<AuthResponseDto>.Success(new AuthResponseDto
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
