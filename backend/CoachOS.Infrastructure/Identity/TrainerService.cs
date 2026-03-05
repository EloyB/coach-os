using CoachOS.Application.Auth.DTOs;
using CoachOS.Application.Common.Interfaces;
using CoachOS.Application.Common.Models;
using CoachOS.Application.Trainers;
using CoachOS.Application.Trainers.Queries.GetTrainers;
using CoachOS.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Identity;

public class TrainerService : ITrainerService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly TokenService _tokenService;
    private readonly IEmailService _emailService;

    public TrainerService(
        UserManager<ApplicationUser> userManager,
        TokenService tokenService,
        IEmailService emailService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _emailService = emailService;
    }

    public async Task<Result<Guid>> InviteAsync(
        Guid organizationId,
        string firstName,
        string lastName,
        string email,
        string inviteBaseUrl,
        CancellationToken ct = default)
    {
        ApplicationUser? existing = await _userManager.FindByEmailAsync(email);
        if (existing is not null)
            return Result<Guid>.Failure("E-mailadres is al in gebruik");

        string inviteToken = Guid.NewGuid().ToString("N");

        ApplicationUser user = new()
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            OrganizationId = organizationId,
            Role = UserRole.Trainer,
            IsActive = false,
            InviteToken = inviteToken,
            InviteTokenExpiry = DateTime.UtcNow.AddHours(72),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Use a placeholder password — will be replaced on invite acceptance
        IdentityResult result = await _userManager.CreateAsync(user, "Placeholder@1!");
        if (!result.Succeeded)
            return Result<Guid>.Failure(result.Errors.Select(e => e.Description));

        string inviteUrl = $"{inviteBaseUrl.TrimEnd('/')}/invite/{inviteToken}";
        await _emailService.SendTrainerInviteAsync(email, firstName, inviteUrl, ct);

        return Result<Guid>.Success(user.Id);
    }

    public async Task<Result<AuthResponseDto>> AcceptInviteAsync(
        string token,
        string password,
        CancellationToken ct = default)
    {
        ApplicationUser? user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.InviteToken == token, ct);

        if (user is null)
            return Result<AuthResponseDto>.Failure("Ongeldige uitnodigingslink");

        if (user.InviteTokenExpiry is null || user.InviteTokenExpiry < DateTime.UtcNow)
            return Result<AuthResponseDto>.Failure("Uitnodigingslink is verlopen");

        string resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        IdentityResult result = await _userManager.ResetPasswordAsync(user, resetToken, password);
        if (!result.Succeeded)
            return Result<AuthResponseDto>.Failure(result.Errors.Select(e => e.Description));

        user.IsActive = true;
        user.InviteToken = null;
        user.InviteTokenExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        (string jwtToken, DateTime expiresAt) = _tokenService.GenerateToken(user);

        return Result<AuthResponseDto>.Success(new AuthResponseDto
        {
            Token = jwtToken,
            ExpiresAt = expiresAt,
            UserId = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            OrganizationId = user.OrganizationId,
            Role = user.Role.ToString()
        });
    }

    public async Task<Result<List<TrainerDto>>> GetTrainersAsync(
        Guid organizationId,
        CancellationToken ct = default)
    {
        List<TrainerDto> trainers = await _userManager.Users
            .AsNoTracking()
            .Where(u => u.OrganizationId == organizationId && u.Role == UserRole.Trainer)
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .Select(u => new TrainerDto
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email!,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync(ct);

        return Result<List<TrainerDto>>.Success(trainers);
    }

    public async Task<Result> DeactivateAsync(
        Guid trainerId,
        Guid organizationId,
        CancellationToken ct = default)
    {
        ApplicationUser? trainer = await _userManager.Users
            .FirstOrDefaultAsync(u => u.Id == trainerId && u.OrganizationId == organizationId, ct);

        if (trainer is null)
            return Result.Failure("Trainer niet gevonden");

        if (trainer.Role != UserRole.Trainer)
            return Result.Failure("Gebruiker is geen trainer");

        trainer.IsActive = false;
        trainer.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(trainer);

        return Result.Success();
    }
}
