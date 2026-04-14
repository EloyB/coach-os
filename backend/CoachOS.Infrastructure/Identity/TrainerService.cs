using System.Security.Cryptography;
using CoachOS.Application.Auth.DTOs;
using CoachOS.Application.Trainers;
using CoachOS.Application.Trainers.DTOs;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;
using CoachOS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CoachOS.Infrastructure.Identity;

public class TrainerService(
    UserManager<ApplicationUser> userManager,
    TokenService tokenService,
    IEmailService emailService,
    ApplicationDbContext context)
    : ITrainerService
{
    public async Task<Result<Guid>> InviteAsync(
        Guid organizationId,
        string firstName,
        string lastName,
        string email,
        string inviteBaseUrl,
        CancellationToken ct = default)
    {
        var existing = await userManager.FindByEmailAsync(email);
        // Block only if an ACTIVE user already has this email.
        // Inactive users (pending/expired invite or deactivated trainer) may be
        // re-invited — we update the existing record to avoid orphaning related
        // entities (LessonSeries.TrainerId etc) that reference the original Id.
        if (existing is { IsActive: true })
            return Result<Guid>.Fail("E-mailadres is al in gebruik");

        var inviteToken = Guid.NewGuid().ToString("N");
        var hashedToken = HashToken(inviteToken);

        ApplicationUser user;
        if (existing is not null)
        {
            existing.FirstName = firstName;
            existing.LastName = lastName;
            existing.OrganizationId = organizationId;
            existing.Role = UserRole.Trainer;
            existing.InviteToken = hashedToken;
            existing.InviteTokenExpiry = DateTime.UtcNow.AddHours(72);
            existing.UpdatedAt = DateTime.UtcNow;
            var updateResult = await userManager.UpdateAsync(existing);
            if (!updateResult.Succeeded)
                return Result<Guid>.Fail(updateResult.Errors.Select(e => e.Description));
            user = existing;
        }
        else
        {
            user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                OrganizationId = organizationId,
                Role = UserRole.Trainer,
                IsActive = false,
                InviteToken = hashedToken,
                InviteTokenExpiry = DateTime.UtcNow.AddHours(72),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Random password — will be replaced on invite acceptance
            var randomPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) + "!A1";
            var createResult = await userManager.CreateAsync(user, randomPassword);
            if (!createResult.Succeeded)
                return Result<Guid>.Fail(createResult.Errors.Select(e => e.Description));
        }

        var inviteUrl = $"{inviteBaseUrl.TrimEnd('/')}/invite/{inviteToken}";
        await emailService.SendTrainerInviteAsync(email, firstName, inviteUrl, ct);

        return Result<Guid>.Ok(user.Id);
    }

    public async Task<Result<AuthResponseDto>> AcceptInviteAsync(
        string token,
        string password,
        CancellationToken ct = default)
    {
        var hashedToken = HashToken(token);
        var user = await userManager.Users
            .FirstOrDefaultAsync(u => u.InviteToken == hashedToken, ct);

        if (user is null)
            return Result<AuthResponseDto>.Fail("Ongeldige uitnodigingslink");

        if (user.InviteTokenExpiry is null || user.InviteTokenExpiry < DateTime.UtcNow)
            return Result<AuthResponseDto>.Fail("Uitnodigingslink is verlopen");

        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, resetToken, password);
        if (!result.Succeeded)
            return Result<AuthResponseDto>.Fail(result.Errors.Select(e => e.Description));

        user.IsActive = true;
        user.InviteToken = null;
        user.InviteTokenExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);

        (var jwtToken, var expiresAt) = tokenService.GenerateToken(user);

        return Result<AuthResponseDto>.Ok(new AuthResponseDto
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
        var trainers = await userManager.Users
            .AsNoTracking()
            .Where(u => u.OrganizationId == organizationId
                        && u.Role == UserRole.Trainer
                        && u.IsActive)
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .Select(u => new TrainerDto
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email!,
                IsActive = u.IsActive,
                InvitePending = u.InviteToken != null,
                CreatedAt = u.CreatedAt,
                LessonCount = context.Lessons
                    .Count(l => l.TrainerId == u.Id && l.OrganizationId == organizationId)
            })
            .ToListAsync(ct);

        return Result<List<TrainerDto>>.Ok(trainers);
    }

    public async Task<Result> DeactivateAsync(
        Guid trainerId,
        Guid organizationId,
        CancellationToken ct = default)
    {
        var trainer = await userManager.Users
            .FirstOrDefaultAsync(u => u.Id == trainerId && u.OrganizationId == organizationId, ct);

        if (trainer is null)
            return Result.Fail("Trainer niet gevonden");

        if (trainer.Role != UserRole.Trainer)
            return Result.Fail("Gebruiker is geen trainer");

        trainer.IsActive = false;
        trainer.UpdatedAt = DateTime.UtcNow;
        await userManager.UpdateAsync(trainer);

        return Result.Ok();
    }

    public async Task<Result> RemoveAsync(
        Guid trainerId,
        Guid organizationId,
        CancellationToken ct = default)
    {
        var trainer = await userManager.Users
            .FirstOrDefaultAsync(u => u.Id == trainerId && u.OrganizationId == organizationId, ct);

        if (trainer is null)
            return Result.Fail("Trainer niet gevonden");

        if (trainer.Role != UserRole.Trainer)
            return Result.Fail("Gebruiker is geen trainer");

        var count = await context.Lessons
            .CountAsync(l => l.TrainerId == trainerId && l.OrganizationId == organizationId, ct);

        if (count > 0)
            return Result.Fail($"Trainer heeft {count} les(sen). Wijs deze eerst toe aan een andere trainer.");

        var result = await userManager.DeleteAsync(trainer);
        return result.Succeeded
            ? Result.Ok()
            : Result.Fail(result.Errors.Select(e => e.Description));
    }

    public async Task<Result> ReassignSeriesAsync(
        Guid fromTrainerId,
        Guid toTrainerId,
        Guid organizationId,
        CancellationToken ct = default)
    {
        var toTrainer = await userManager.Users
            .FirstOrDefaultAsync(u => u.Id == toTrainerId && u.OrganizationId == organizationId, ct);

        if (toTrainer is null)
            return Result.Fail("Doeltrainer niet gevonden");

        if (toTrainer.Role != UserRole.Trainer)
            return Result.Fail("Doelgebruiker is geen trainer");

        if (!toTrainer.IsActive)
            return Result.Fail("Doeltrainer is niet actief");

        await context.Lessons
            .Where(l => l.TrainerId == fromTrainerId && l.OrganizationId == organizationId)
            .ExecuteUpdateAsync(s => s.SetProperty(l => l.TrainerId, toTrainerId), ct);

        return Result.Ok();
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToHexStringLower(bytes);
    }
}
